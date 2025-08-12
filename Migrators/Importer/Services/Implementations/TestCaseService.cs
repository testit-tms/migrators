using System.Text.RegularExpressions;
using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services.Implementations;

internal class TestCaseService(
    ILogger<TestCaseService> logger,
    IClientAdapter clientAdapter,
    IAdapterHelper adapterHelper,
    IParserService parserService,
    IParameterService parameterService,
    IBaseWorkItemService baseWorkItemService,
    IAttachmentService attachmentService)
    : ITestCaseService
{
    private Dictionary<Guid, TmsAttribute> _attributesMap = new();
    private Dictionary<Guid, Guid> _sectionsMap = new();
    private Dictionary<Guid, Guid> _sharedSteps = new();

    public async Task<List<string>> ImportTestCases(Guid projectId, IEnumerable<Guid> testCases, Dictionary<Guid, Guid> sections,
        Dictionary<Guid, TmsAttribute> attributes, Dictionary<Guid, Guid> sharedSteps)
    {
        _attributesMap = attributes;
        _sectionsMap = sections;
        _sharedSteps = sharedSteps;

        logger.LogInformation("Importing test cases");

        // var allTestCases = new List<string>();
        var notImportedTestCases = new List<string>();

        foreach (var testCase in testCases)
        {
            var tc = await parserService.GetTestCase(testCase);
            // allTestCases.Add(tc.Name);
            try
            {
                await ImportTestCase(projectId, tc);
            }
            catch (Exception e)
            {
                logger.LogError("Could not import test case {Name} with error {Message}: {InnerMessage}; {Stack}",
                    tc.Name, e.Message, e.InnerException?.Message, e.StackTrace);
                notImportedTestCases.Add(tc.Name);
            }
        }

        return notImportedTestCases;
    }

    private async Task ImportTestCase(Guid projectId, TestCase testCase)
    {
        var sectionId = _sectionsMap[testCase.SectionId];

        logger.LogDebug("Importing test case {Name} to section {Id}", testCase.Name, sectionId);

        testCase.Attributes = await baseWorkItemService.ConvertAttributes(testCase.Attributes, _attributesMap);

        logger.LogInformation("Try to parse shared steps");
        testCase.Steps.Where(s => s.SharedStepId != null)
            .ToList()
            .ForEach(s =>
            {
                try
                {
                    if (_sharedSteps.TryGetValue(s.SharedStepId!.Value, out var sharedStepId))
                    {
                        s.SharedStepId = sharedStepId;
                    }
                    else
                    {
                        s.SharedStepId = null;
                    }
                }
                catch (Exception e)
                {
                    logger.LogWarning("Exception {Exception} when trying to parse shared step {Name} with error {Message}",
                        e, s.SharedStepId!.Value, e.Message);
                    s.SharedStepId = null;
                }
            });

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
        logger.LogInformation("Trying to select attachments");
        tmsTestCase.Attachments = attachments.Select(a => a.Value.ToString()).ToList();

        logger.LogInformation("Trying to add attachments to steps");
        tmsTestCase.Steps = baseWorkItemService.AddAttachmentsToSteps(tmsTestCase.Steps, attachments);
        tmsTestCase.PreconditionSteps = baseWorkItemService.AddAttachmentsToSteps(tmsTestCase.PreconditionSteps, attachments);
        tmsTestCase.PostconditionSteps = baseWorkItemService.AddAttachmentsToSteps(tmsTestCase.PostconditionSteps, attachments);

        logger.LogInformation("Trying to import tc call");
        var result = await adapterHelper.RetryCaller(async () =>
        await clientAdapter.ImportTestCase(projectId, sectionId, tmsTestCase));

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
