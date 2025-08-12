using System.Text;
using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services.Helpers;
using ZephyrScaleServerExporter.Services.TestCase.Helpers;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporter.Services.TestCase.Implementations;

public class TestCaseConvertService(
    ILogger<TestCaseConvertService> logger,
    IClient client,
    ITestCaseServiceHelper testCaseServiceHelper,
    IStepService stepService,
    ITestCaseAttachmentsService testCaseAttachmentsService,
    ITestCaseAttributesService testCaseAttributesService,
    ITestCaseAdditionalLinksService testCaseAdditionalLinksService,
    IParameterService parameterService) 
    : ITestCaseConvertService
{
    
    private const int Duration = 10000;

    public async Task<global::Models.TestCase?> ConvertTestCase(
        ZephyrTestCase zephyrTestCase,
        SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames,
        Attribute ownersAttribute)
    {
        logger.LogInformation("Converting test case {Name}", zephyrTestCase.Name);

        if (zephyrTestCase.Key == null)
        {
            logger.LogInformation("Skipping test case {Name}", zephyrTestCase.Name);

            return null;
        }
        var sectionId = ConvertFolders(zephyrTestCase.Folder, sectionData);
        var testCaseId = Guid.NewGuid();
        var attributes = testCaseAttributesService.CalculateAttributes(zephyrTestCase, attributeMap, requiredAttributeNames);
        var description = Utils.ExtractAttachments(zephyrTestCase.Description);
        var precondition = Utils.ExtractAttachments(zephyrTestCase.Precondition);
        
        
        // API Calls
        var parseTask = ParseTestCaseOwner(zephyrTestCase, attributes, ownersAttribute);
        var addLinksTask = testCaseAdditionalLinksService.GetAdditionalLinks(zephyrTestCase);
        // many API Call (downloading)
        var attachmentsTask = testCaseAttachmentsService.FillAttachments(testCaseId, zephyrTestCase, description);
        // many API Calls (+downloading)
        var convertStepsDataTask = ConvertStepsData(testCaseId, zephyrTestCase);

        await Task.WhenAll(parseTask, convertStepsDataTask, addLinksTask, attachmentsTask);
        
        await parseTask;
        var additionalLinks = await addLinksTask;
        var attachments = await attachmentsTask;

        var stepsData = await convertStepsDataTask;
        
        var iterations = stepsData.Iterations;
        var steps = stepsData.Steps;
        steps.ForEach(s =>
        {
            Utils.AddIfUnique(attachments, s.GetAllAttachments());
        });
        
        // API Call
        var preconditionAttachments = 
            await testCaseAttachmentsService.CalcPreconditionAttachments(testCaseId, precondition, attachments);

        var testCase = new global::Models.TestCase
        {
            Id = testCaseId,
            Description = Utils.ExtractHyperlinks(description.Description),
            State = testCaseServiceHelper.ConvertStatus(zephyrTestCase.Status),
            Priority = testCaseServiceHelper.ConvertPriority(zephyrTestCase.Priority),
            Steps = steps,
            PreconditionSteps = string.IsNullOrEmpty(zephyrTestCase.Precondition)
                ? []
                :
                [
                    new Step
                    {
                        Action = Utils.ConvertingFormatCharacters(precondition.Description),
                        Expected = string.Empty,
                        ActionAttachments = preconditionAttachments,
                        TestData = string.Empty,
                        TestDataAttachments = [],
                        ExpectedAttachments = []
                    }
                ],
            PostconditionSteps = [],
            Duration = Duration,
            Attributes = attributes,
            Tags = zephyrTestCase.Labels ?? [],
            Attachments = testCaseServiceHelper.ExcludeDuplicates(attachments),
            Iterations = testCaseServiceHelper.SanitizeIterations(iterations),
            Links = additionalLinks,
            Name = zephyrTestCase.Name,
            SectionId = sectionId
        };

        testCaseServiceHelper.ExcludeLongTags(testCase);

        return testCase;
    }
    
    private Guid ConvertFolders(string? stringFolders, SectionData sectionData)
    {
        logger.LogInformation("Converting folders");

        var sectionKey = new StringBuilder(Constants.MainFolderKey);


        if (stringFolders == null)
        {
            return sectionData.SectionMap[sectionKey.ToString()];
        }

        if (sectionData.AllSections.ContainsKey(sectionKey + stringFolders))
        {
            return sectionData.SectionMap[sectionKey + stringFolders];
        }

        var lastSectionId = sectionData.SectionMap[sectionKey.ToString()];
        var folders = stringFolders.Split('/');

        foreach (var folder in folders)
        {
            if (string.IsNullOrEmpty(folder))
            {
                continue;
            }

            if (!sectionData.AllSections.ContainsKey(sectionKey + "/" + folder))
            {
                var section = new Section
                {
                    Id = Guid.NewGuid(),
                    Name = folder,
                    Sections = new List<Section>(),
                    PostconditionSteps = new List<Step>(),
                    PreconditionSteps = new List<Step>()
                };

                if (!sectionData.AllSections.ContainsKey(sectionKey.ToString()))
                {
                    logger.LogInformation("The section \"{Key}\" cannot be obtained from the all sections map", sectionKey);

                    continue;
                }

                sectionData.AllSections[sectionKey.ToString()].Sections.Add(section);
                sectionData.SectionMap.Add(sectionKey + "/" + folder, section.Id);
                sectionData.AllSections.Add(sectionKey + "/" + folder, section);
            }

            sectionKey.Append("/" + folder);
            lastSectionId = sectionData.SectionMap[sectionKey.ToString()];
        }

        return lastSectionId;
    }
    
    private async Task<StepsData> ConvertStepsData(Guid testCaseId,
        ZephyrTestCase zephyrTestCase)
    {
        var iterations = await parameterService.ConvertParameters(zephyrTestCase.Key!);
        if (zephyrTestCase.TestScript != null)
        {
            return await stepService.ConvertSteps(testCaseId, zephyrTestCase.TestScript, iterations);
        }
        return new StepsData { Iterations = iterations, Steps = [] };
    }
    
    /// <summary>
    /// Parse owner's metadata to <see cref="attributes"/>, else skip.
    /// Updates <see cref="ownersAttribute"/>.
    /// </summary>
    private async Task ParseTestCaseOwner(ZephyrTestCase? zephyrTestCase, 
        List<CaseAttribute> attributes, Attribute ownersAttribute)
    {
        if (zephyrTestCase == null || string.IsNullOrEmpty(zephyrTestCase.OwnerKey))
        {
            return;
        }
        var owner = await client.GetOwner(zephyrTestCase.OwnerKey);
        if (owner == null)
        {
            return;
        }
        Utils.AddIfUnique(ownersAttribute.Options, owner.DisplayName);
        attributes.Add(
            new CaseAttribute
            {
                Id = ownersAttribute.Id,
                Value = owner.DisplayName
            }
        );
    }
    
}