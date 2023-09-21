using AzureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Constants = AzureExporter.Models.Constants;

namespace AzureExporter.Services;

public class SharedStepService : ISharedStepService
{
    private readonly ILogger<SharedStepService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;

    public SharedStepService(ILogger<SharedStepService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public async Task<Dictionary<int, SharedStep>> ConvertSharedSteps(Guid projectId, Guid sectionId)
    {
        _logger.LogInformation("Converting shared steps");

        var workItems = await _client.GetWorkItems(Constants.SharedStepType);

        _logger.LogDebug("Found {@WorkItems} shared steps", workItems.Count);

        var sharedSteps = new Dictionary<int, SharedStep>();

        foreach (var workItem in workItems)
        {
            var sharedStep = await _client.GetWorkItemById(workItem.Id);

            _logger.LogDebug("Found shared step: {Id}", sharedStep.Id);

            var steps = await _stepService.ConvertSteps(
                sharedStep.Fields["Microsoft.VSTS.TCM.Steps"] as string, new Dictionary<int, Guid>());

            _logger.LogDebug("Found {@Steps} steps", steps.Count);

            var step = new SharedStep
            {
                Id = Guid.NewGuid(),
                Name = sharedStep.Fields["System.Title"] as string,
                Steps = steps,
                Description = "",
                State = StateType.Ready,
                Priority = PriorityType.Medium,
                Attributes = new List<CaseAttribute>(),
                Links = new List<Link>(),
                Attachments = new List<string>(),
                SectionId = sectionId,
                Tags = new List<string>()
            };

            _logger.LogDebug("Converted shared step: {@Step}", step);

            sharedSteps.Add(workItem.Id, step);
        }

        return sharedSteps;
    }
}
