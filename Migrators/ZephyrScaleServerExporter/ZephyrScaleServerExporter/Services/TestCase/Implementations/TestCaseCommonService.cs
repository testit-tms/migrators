using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using ZephyrScaleServerExporter.AttrubuteMapping;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Models.TestCases.Export;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;


namespace ZephyrScaleServerExporter.Services.TestCase.Implementations;

internal class TestCaseCommonService(
    IDetailedLogService detailedLogService,
    ILogger<TestCaseCommonService> logger,
    ITestCaseConvertService testCaseConvertService,
    IStatusService statusService,
    IMappingConfigReader mappingConfigReader,
    IWriteService writeService,
    ITestCaseErrorLogService testCaseErrorLogService
    ) : ITestCaseCommonService
{
    public async Task<TestCaseExportRequiredModel> PrepareForTestCasesExportAsync(
        Dictionary<string, Attribute> attributeMap,
        string projectId)
    {
        var ownersAttribute = new Attribute()
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = []
        };
        var statusData = await statusService.ConvertStatuses(projectId);

        attributeMap.Add(statusData.StatusAttribute.Name, statusData.StatusAttribute);

        detailedLogService.LogInformation("Get all attribute values {Attrs}: ", attributeMap.Values);

        var requiredAttributeNames = attributeMap.Values
            .Where(a => a.IsRequired).Select(a => a.Name).ToList();
        detailedLogService.LogInformation("Get all required attributes: {List}", requiredAttributeNames);

        return new TestCaseExportRequiredModel
        {
            OwnersAttribute = ownersAttribute,
            StatusData = statusData,
            RequiredAttributeNames = requiredAttributeNames
        };
    }

    public TestCaseData PrepareTestCaseIdsData(
        Dictionary<string, Attribute> attributeMap,
        Attribute ownersAttribute,
        List<Guid> testCaseIds)
    {
        var tmsAttributes = attributeMap.Values.ToList();

        if (ownersAttribute.Options.Count != 0)
        {
            tmsAttributes.Add(ownersAttribute);
        }

        RemapStatusAttributeOptions(tmsAttributes, "Состояние");

        return new TestCaseData
        {
            TestCaseIds = testCaseIds,
            Attributes = tmsAttributes
        };
    }

    /// <summary>
    /// change status attribute in place to it's remapping from mapping.json file
    /// </summary>
    private void RemapStatusAttributeOptions(List<Attribute> tmsAttributes, string attributeName)
    {
        try
        {
            var attribute = tmsAttributes.Find(a => a.Name == attributeName);

            attribute!.Options = attribute.Options.Select(
                option => RemapValue(option, attributeName)
                ).Distinct().ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while mapping attribute value or read the file");
        }
    }

    private string RemapValue(string option, string attributeName)
    {
        mappingConfigReader.InitOnce("mapping.json", "");
        var mappingValue = mappingConfigReader
            .GetMappingForValue(option ?? "", attributeName);
        if (mappingValue != null)
        {
            detailedLogService.LogInformation($"Map {option} to {mappingValue} in the attribute {attributeName}");
            return mappingValue;
        }

        return option!;
    }

    public async Task<List<Guid>> WriteTestCasesAsync(
        List<ZephyrTestCase> cases,
        SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames,
        Attribute ownersAttribute)
    {
        List<Guid> testCaseIds = [];
        var tasks = cases
            .Select(x =>
                ConvertAndWriteCaseAsync(x, sectionData,
                    attributeMap, requiredAttributeNames, ownersAttribute)).ToList();
        try
        {
            var results = await Task.WhenAll(tasks);
            testCaseIds = results.OfType<Guid>().ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while writing test cases batch.");
            testCaseErrorLogService.LogError(ex, "An error occurred during Task.WhenAll in WriteTestCasesAsync while processing a batch.", null, cases);
        }
        return testCaseIds;
    }

    private async Task<Guid?> ConvertAndWriteCaseAsync(
        ZephyrTestCase zephyrTestCase,
        SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames,
        Attribute ownersAttribute)
    {
        try
        {
            var testCase = await testCaseConvertService.ConvertTestCase(
                zephyrTestCase,
                sectionData,
                attributeMap,
                requiredAttributeNames,
                ownersAttribute);
            if (testCase == null)
            {
                logger.LogError("Conversion of ZephyrTestCase {Key} resulted in null, skipping write.", zephyrTestCase.Key);
                return null;
            }
            await writeService.WriteTestCase(testCase);
            return testCase.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during ConvertAndWriteCaseAsync for Test Case Key: {Key}", zephyrTestCase.Key);
            testCaseErrorLogService.LogError(ex, $"Error processing individual test case in ConvertAndWriteCaseAsync.", problematicTestCase: zephyrTestCase);
            throw;
        }
    }

    public async Task<List<ZephyrTestCase>> GetTestCasesByConfig(IOptions<AppConfig> config,
        IClient client,
        int startAt, int maxResults, string statuses)
    {
        var filter = new StringBuilder("");
        if (config.Value.Zephyr.FilterName != string.Empty)
        {
            var nameFilter = config.Value.Zephyr.FilterName;
            filter.Append($" AND testCase.name LIKE \"{nameFilter}\" ");
        }
        if (config.Value.Zephyr.FilterSection != string.Empty)
        {
            var section = config.Value.Zephyr.FilterSection;
            filter.Append($" AND testCase.folderTreeId IN ({section}) ");
        }

        if (filter.Length > 0)
        {
            return await client.GetTestCasesWithFilter(startAt, maxResults,
                statuses, filter.ToString());
        }

        return await client.GetTestCases(startAt, maxResults,
            statuses);
    }

    public async Task<List<ZephyrTestCase>> GetArchivedTestCases(IOptions<AppConfig> config, IClient client,
        int startAt, int maxResults, string statuses)
    {
        var archivedResults = await client.GetTestCasesArchived(startAt, maxResults,
            statuses);

        archivedResults.ForEach(x =>
        {
            x.Labels ??= [];
            x.CustomFields ??= new Dictionary<string, object>();

            x.Labels.Add("Archived");
            x.CustomFields.Add("Archived", "true");
            x.IsArchived = true;
        });

        return archivedResults;
    }

}
