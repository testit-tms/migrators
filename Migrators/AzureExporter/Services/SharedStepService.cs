using AzureExporter.Client;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Models;
using Constants = AzureExporter.Models.Constants;
using Link = Models.Link;

namespace AzureExporter.Services;

public class SharedStepService : WorkItemBaseService, ISharedStepService
{
    private readonly ILogger<SharedStepService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;
    private readonly ILinkService _linkService;

    public SharedStepService(ILogger<SharedStepService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService, ILinkService linkService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
        _linkService = linkService;
    }

    public async Task<Dictionary<int, SharedStep>> ConvertSharedSteps(Guid projectId, Guid sectionId,
        Dictionary<string, Guid> attributeMap)
    {
        _logger.LogInformation("Converting shared steps");

        var workItemIds = await _client.GetWorkItemIds(Constants.SharedStepType);

        _logger.LogDebug("Found {@WorkItems} shared steps", workItemIds.Count);

        var sharedSteps = new Dictionary<int, SharedStep>();

        foreach (var workItemId in workItemIds)
        {
            var sharedStep = await _client.GetWorkItemById(workItemId);

            _logger.LogDebug("Found shared step: {Id}", sharedStep.Id);

            var steps = _stepService.ConvertSteps(sharedStep.Steps, new Dictionary<int, Guid>());

            _logger.LogDebug("Found {@Steps} steps", steps.Count);

            var sharedStepGuid = Guid.NewGuid();
            var tmsAttachments = await _attachmentService.DownloadAttachments(sharedStep.Attachments, sharedStepGuid);
            var links = _linkService.CovertLinks(sharedStep.Links);

            var step = new SharedStep
            {
                Id = sharedStepGuid,
                Name = sharedStep.Title,
                Steps = steps,
                Description = sharedStep.Description,
                State = StateType.NotReady,
                Priority = ConvertPriority(sharedStep.Priority),
                Attributes = new List<CaseAttribute>
                {
                    new()
                    {
                        Id = attributeMap[Constants.IterationAttributeName],
                        Value = sharedStep.IterationPath
                    },
                    new()
                    {
                        Id = attributeMap[Constants.StateAttributeName],
                        Value = sharedStep.State
                    }
                },
                Links = links,
                Attachments = tmsAttachments,
                SectionId = sectionId,
                Tags = ConvertTags(sharedStep.Tags)
            };

            _logger.LogDebug("Converted shared step: {@Step}", step);

            sharedSteps.Add(workItemId, step);
        }

        return sharedSteps;
    }

    private List<Link> ConvertLinks(List<WorkItemRelation> relations)
    {
        var links = new List<Link>();

        foreach (var relation in relations)
        {
            if (relation.Rel.Equals("ArtifactLink"))
            {
                links.Add(
                    new Link
                    {
                        Url = relation.Url,
                        Description = relation.Attributes["name"] as string
                    }
                );
            }
        }

        return links;
    }
}
