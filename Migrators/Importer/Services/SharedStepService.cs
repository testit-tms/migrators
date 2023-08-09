using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services;

public class SharedStepService : ISharedStepService
{
    private readonly ILogger<SharedStepService> _logger;
    private readonly IClient _client;
    private readonly IParserService _parserService;
    private Dictionary<Guid, TmsAttribute> _attributesMap;
    private Dictionary<Guid, Guid> _sectionsMap;
    private readonly Dictionary<Guid, Guid> _sharedSteps = new();

    public SharedStepService(ILogger<SharedStepService> logger, IClient client, IParserService parserService)
    {
        _logger = logger;
        _client = client;
        _parserService = parserService;
    }

    public async Task<Dictionary<Guid, Guid>> ImportSharedSteps(IEnumerable<Guid> sharedSteps,
        Dictionary<Guid, Guid> sections, Dictionary<Guid, TmsAttribute> attributes)
    {
        _attributesMap = attributes;
        _sectionsMap = sections;

        _logger.LogInformation("Importing shared steps");

        foreach (var sharedStep in sharedSteps)
        {
            var step = await _parserService.GetSharedStep(sharedStep);
            await ImportSharedStep(step);
        }

        return _sharedSteps;
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
}
