using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services.Implementations;

internal class SharedStepService(
    ILogger<SharedStepService> logger,
    IClientAdapter clientAdapter,
    IParserService parserService,
    IBaseWorkItemService baseWorkItemService,
    IAttachmentService attachmentService)
    : ISharedStepService
{
    private readonly Dictionary<Guid, Guid> _sharedSteps = new();
    private Dictionary<Guid, TmsAttribute> _attributesMap = new();
    private Dictionary<Guid, Guid> _sectionsMap = new();

    public async Task<Dictionary<Guid, Guid>> ImportSharedSteps(Guid projectId, IEnumerable<Guid> sharedSteps,
        Dictionary<Guid, Guid> sections, Dictionary<Guid, TmsAttribute> attributes)
    {
        _attributesMap = attributes;
        _sectionsMap = sections;

        logger.LogInformation("Importing shared steps");

        foreach (var sharedStep in sharedSteps)
        {
            var step = await parserService.GetSharedStep(sharedStep);
            await ImportSharedStep(projectId, step);
        }

        return _sharedSteps;
    }

    private async Task ImportSharedStep(Guid projectId, SharedStep step)
    {
        step.Attributes = await baseWorkItemService.ConvertAttributes(step.Attributes, _attributesMap);
        var attachments = await attachmentService.GetAttachments(step.Id, step.Attachments);
        step.Attachments = attachments.Select(a => a.Value.ToString()).ToList();
        step.Steps = baseWorkItemService.AddAttachmentsToSteps(step.Steps, attachments);

        var sectionId = _sectionsMap[step.SectionId];

        logger.LogDebug("Importing shared step {Name} to section {SectionId}",
            step.Name,
            sectionId);

        var stepId = await clientAdapter.ImportSharedStep(projectId, sectionId, step);

        _sharedSteps.Add(step.Id, stepId);

        logger.LogDebug("Imported shared step {Name} with id {Id} to section {SectionId}",
            step.Name,
            stepId,
            sectionId);
    }
}
