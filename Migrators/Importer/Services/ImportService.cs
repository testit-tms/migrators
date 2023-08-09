using System.Text.RegularExpressions;
using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services;

public class ImportService : IImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly IParserService _parserService;
    private readonly IClient _client;
    private readonly IAttributeService _attributeService;
    private readonly IParameterService _parameterService;

    private readonly Dictionary<Guid, Guid> _sectionsMap = new();
    private Dictionary<Guid, TmsAttribute> _attributesMap = new();
    private readonly Dictionary<Guid, Guid> _sharedSteps = new();

    public ImportService(ILogger<ImportService> logger,
        IParserService parserService,
        IClient client,
        IAttributeService attributeService,
        IParameterService parameterService)
    {
        _logger = logger;
        _parserService = parserService;
        _client = client;
        _attributeService = attributeService;
        _parameterService = parameterService;
    }

    public async Task ImportProject()
    {
        _logger.LogInformation("Importing project");

        var mainJsonResult = await _parserService.GetMainFile();

        _logger.LogInformation("Creating project {Name}", mainJsonResult.ProjectName);

        await _client.CreateProject(mainJsonResult.ProjectName);

        var rootSectionId = await _client.GetRootSectionId();

        _logger.LogInformation("Importing sections");

        foreach (var section in mainJsonResult.Sections)
        {
            await ImportSection(rootSectionId, section);
        }

        _logger.LogInformation("Importing attributes");

        _attributesMap = await _attributeService.ImportAttributes(mainJsonResult.Attributes);

        _logger.LogInformation("Importing shared steps");

        foreach (var sharedStep in mainJsonResult.SharedSteps)
        {
            var step = await _parserService.GetSharedStep(sharedStep);
            await ImportSharedStep(step);
        }

        _logger.LogInformation("Importing test cases");

        foreach (var testCase in mainJsonResult.TestCases)
        {
            var caseResult = await _parserService.GetTestCase(testCase);
            var sectionId = _sectionsMap[caseResult.SectionId];

            _logger.LogDebug("Importing test case {Name} to section {Id}", caseResult.Name, sectionId);

            caseResult.Attributes = (from attribute in caseResult.Attributes
                let atr = _attributesMap[attribute.Id]
                let value = string.Equals(atr.Type, "options", StringComparison.InvariantCultureIgnoreCase)
                    ? Enumerable.FirstOrDefault<TmsAttributeOptions>(atr.Options, o => o.Value == attribute.Value)?.Id
                        .ToString()
                    : attribute.Value
                select new CaseAttribute() { Id = atr.Id, Value = value }).ToArray();

            caseResult.Steps.Where(s => s.SharedStepId != null)
                .ToList()
                .ForEach(s => s.SharedStepId = _sharedSteps[s.SharedStepId!.Value]);

            var tmsTestCase = TmsTestCase.Convert(caseResult);

            var iterations = new List<TmsIterations>();
            var isStepChanged = false;

            foreach (var iteration in caseResult.Iterations)
            {
                var parameters = await _parameterService.CreateParameters(iteration.Parameters);

                if (!isStepChanged)
                {
                    caseResult.Steps.ToList().ForEach(
                        s =>
                        {
                            s.Action = AddParameter(s.Action, parameters);
                            s.Expected = AddParameter(s.Expected, parameters);
                        });

                    isStepChanged = true;
                }

                iterations.Add(new TmsIterations()
                {
                    Parameters = parameters.Select(p => p.Id).ToList()
                });
            }

            tmsTestCase.TmsIterations = iterations;

            tmsTestCase.Attachments = await GetAttachments(caseResult.Id, caseResult.Attachments);

            await _client.ImportTestCase(sectionId, tmsTestCase);

            _logger.LogDebug("Imported test case {Name} to section {Id}", caseResult.Name, sectionId);
        }

        _logger.LogInformation("Project imported");
    }

    private async Task ImportSection(Guid parentSectionId, Section section)
    {
        _logger.LogDebug("Importing section {Name} to parent section {Id}",
            section.Name,
            parentSectionId);

        var sectionId = await _client.ImportSection(parentSectionId, section);
        _sectionsMap.Add(section.Id, sectionId);

        foreach (var sectionSection in section.Sections)
        {
            await ImportSection(sectionId, sectionSection);
        }

        _logger.LogDebug("Imported section {Name} to parent section {Id}",
            section.Name,
            parentSectionId);
    }

    private async Task ImportSharedStep(SharedStep step)
    {
        step.Attributes = (from attribute in step.Attributes
            let atr = _attributesMap[attribute.Id]
            let value = string.Equals(atr.Type, "options", StringComparison.InvariantCultureIgnoreCase)
                ? Enumerable.FirstOrDefault<TmsAttributeOptions>(atr.Options, o => o.Value == attribute.Value)?.Id
                    .ToString()
                : attribute.Value
            select new CaseAttribute() { Id = atr.Id, Value = value }).ToArray();

        step.Attachments = await GetAttachments(step.Id, step.Attachments);

        var sectionId = _sectionsMap[step.SectionId];
        var stepId = await _client.ImportSharedStep(sectionId, step);

        _logger.LogDebug("Importing shared step {Name} with id {Id} to section {SectionId}",
            step.Name,
            stepId,
            sectionId);

        _sharedSteps.Add(step.Id, stepId);

        _logger.LogDebug("Imported shared step {Name} with id {Id} to section {SectionId}",
            step.Name,
            stepId,
            sectionId);
    }

    private async Task<string[]> GetAttachments(Guid workItemId, IEnumerable<string> attachments)
    {
        List<string> ids = new();

        foreach (var attachment in attachments)
        {
            var stream = await _parserService.GetAttachment(workItemId, attachment);
            var id = await _client.UploadAttachment(attachment, stream);
            ids.Add(id.ToString());
        }

        return ids.ToArray();
    }

    private string AddParameter(string line, IEnumerable<TmsParameter> parameters)
    {
        if (string.IsNullOrEmpty(line)) return line;

        var regexp = new Regex("<<<(.*?)>>>");
        var match = regexp.Match(line).Groups;

        foreach (Group group in match)
        {
            var param = parameters.FirstOrDefault(p =>
                string.Equals(p.Name, group.Value, StringComparison.InvariantCultureIgnoreCase));
            if (param is null) continue;

            var repl =
                $"<span class=\"mention\" data-index=\"0\" data-denotation-char=\"%\" data-id=\"{param.ParameterKeyId}\"" +
                $" data-value=\"{param.Name}\"> <span contenteditable=\"false\"><span class=\"ql-mention-denotation-char\">" +
                $"%</span>{param.Name}</span> </span>";

            line = regexp.Replace(line, repl);
        }

        return line;
    }
}
