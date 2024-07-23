using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services;

public class SharedStepService : BaseWorkItemService, ISharedStepService
{
    private readonly ILogger<SharedStepService> _logger;
    private readonly IClient _client;
    private readonly IParserService _parserService;
    private readonly IAttachmentService _attachmentService;
    private Dictionary<Guid, TmsAttribute> _attributesMap;
    private Dictionary<Guid, Guid> _sectionsMap;
    private readonly Dictionary<Guid, Guid> _sharedSteps = new();

    public SharedStepService(ILogger<SharedStepService> logger, IClient client, IParserService parserService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _parserService = parserService;
        _attachmentService = attachmentService;
    }

    public async Task<Dictionary<Guid, Guid>> ImportSharedSteps(Guid projectId, IEnumerable<Guid> sharedSteps,
        Dictionary<Guid, Guid> sections, Dictionary<Guid, TmsAttribute> attributes)
    {
        _attributesMap = attributes;
        _sectionsMap = sections;

        _logger.LogInformation("Importing shared steps");

        foreach (var sharedStep in sharedSteps)
        {
            var step = await _parserService.GetSharedStep(sharedStep);
            await ImportSharedStep(projectId, step);
        }

        return _sharedSteps;
    }

    private async Task ImportSharedStep(Guid projectId, SharedStep step)
    {
        step.Attributes = ConvertAttributes(step.Attributes, _attributesMap);
        var attachments = await _attachmentService.GetAttachments(step.Id, step.Attachments);
        step.Attachments = attachments.Select(a => a.Value.ToString()).ToList();
        step.Steps = AddAttachmentsToSteps(step.Steps, attachments);

        var sectionId = _sectionsMap[step.SectionId];

        _logger.LogDebug("Importing shared step {Name} to section {SectionId}",
            step.Name,
            sectionId);

        var stepId = await _client.ImportSharedStep(projectId, sectionId, step);

        _sharedSteps.Add(step.Id, stepId);

        _logger.LogDebug("Imported shared step {Name} with id {Id} to section {SectionId}",
            step.Name,
            stepId,
            sectionId);
    }
}
