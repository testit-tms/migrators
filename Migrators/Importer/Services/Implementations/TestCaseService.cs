using System.Text.RegularExpressions;
using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services.Implementations;

internal class TestCaseService(
    ILogger<TestCaseService> logger,
    IClientAdapter clientAdapter,
    IParserService parserService,
    IParameterService parameterService,
    IAttachmentService attachmentService)
    : BaseWorkItemService, ITestCaseService
{
    private Dictionary<Guid, TmsAttribute> _attributesMap = new();
    private Dictionary<Guid, Guid> _sectionsMap = new();
    private Dictionary<Guid, Guid> _sharedSteps = new();

    public async Task ImportTestCases(Guid projectId, IEnumerable<Guid> testCases, Dictionary<Guid, Guid> sections,
        Dictionary<Guid, TmsAttribute> attributes, Dictionary<Guid, Guid> sharedSteps)
    {
        _attributesMap = attributes;
        _sectionsMap = sections;
        _sharedSteps = sharedSteps;

        logger.LogInformation("Importing test cases");

        foreach (var testCase in testCases)
        {
            var tc = await parserService.GetTestCase(testCase);
            try
            {
                await ImportTestCase(projectId, tc);
            }
            catch (Exception e)
            {
                logger.LogError("Could not import test case {Name} with error {Message}", tc.Name, e.Message);
            }
        }
    }

    private async Task ImportTestCase(Guid projectId, TestCase testCase)
    {
        var sectionId = _sectionsMap[testCase.SectionId];

        logger.LogDebug("Importing test case {Name} to section {Id}", testCase.Name, sectionId);

        testCase.Attributes = ConvertAttributes(testCase.Attributes, _attributesMap);

        testCase.Steps.Where(s => s.SharedStepId != null)
            .ToList()
            .ForEach(s => s.SharedStepId = _sharedSteps[s.SharedStepId!.Value]);

        var tmsTestCase = TmsTestCase.Convert(testCase);

        var iterations = new List<TmsIterations>();
        var isStepChanged = false;

        foreach (var iteration in testCase.Iterations)
        {
            var parameters = await parameterService.CreateParameters(iteration.Parameters);

            if (!isStepChanged)
            {
                tmsTestCase.Steps.ToList().ForEach(
                    s =>
                    {
                        s.Action = AddParameter(s.Action, parameters);
                        s.Expected = AddParameter(s.Expected, parameters);
                        s.TestData = AddParameter(s.TestData, parameters);
                    });

                isStepChanged = true;
            }

            iterations.Add(new TmsIterations
            {
                Parameters = parameters.Select(p => p.Id).ToList()
            });
        }

        tmsTestCase.TmsIterations = iterations;

        var attachments = await attachmentService.GetAttachments(testCase.Id, testCase.Attachments);
        tmsTestCase.Attachments = attachments.Select(a => a.Value.ToString()).ToList();

        tmsTestCase.Steps = AddAttachmentsToSteps(tmsTestCase.Steps, attachments);
        tmsTestCase.PreconditionSteps = AddAttachmentsToSteps(tmsTestCase.PreconditionSteps, attachments);
        tmsTestCase.PostconditionSteps = AddAttachmentsToSteps(tmsTestCase.PostconditionSteps, attachments);

        await clientAdapter.ImportTestCase(projectId, sectionId, tmsTestCase);

        logger.LogDebug("Imported test case {Name} to section {Id}", testCase.Name, sectionId);
    }

    private static string AddParameter(string line, IEnumerable<TmsParameter> parameters)
    {
        if (string.IsNullOrEmpty(line)) return line;

        var regexp = new Regex("<<<(.*?)>>>");
        var matches = regexp.Matches(line);

        foreach (var match in matches)
        {
            var param = parameters.FirstOrDefault(p =>
                string.Equals("<<<" + p.Name + ">>>", match.ToString(), StringComparison.InvariantCultureIgnoreCase));
            if (param is null) continue;

            var repl =
                $"<span class=\"mention\" data-index=\"0\" data-denotation-char=\"%\" data-id=\"{param.ParameterKeyId}\"" +
                $" data-value=\"{param.Name}\"> <span contenteditable=\"false\"><span class=\"ql-mention-denotation-char\">" +
                $"%</span>{param.Name}</span> </span>";

            line = line.Replace("<<<" + param.Name + ">>>", repl);
        }

        return line;
    }
}