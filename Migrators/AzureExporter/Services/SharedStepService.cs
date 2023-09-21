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

    public async Task<Dictionary<int, SharedStep>> ConvertSharedSteps(Guid projectId, Guid sectionId,
        Dictionary<string, Guid> attributeMap)
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
                GetValueOfField(sharedStep.Fields, "Microsoft.VSTS.TCM.Steps"),
                new Dictionary<int, Guid>()
            );

            _logger.LogDebug("Found {@Steps} steps", steps.Count);

            var step = new SharedStep
            {
                Id = Guid.NewGuid(),
                Name = GetValueOfField(sharedStep.Fields, "System.Title"),
                Steps = steps,
                Description = GetValueOfField(sharedStep.Fields, "System.Description"),
                State = StateType.Ready,
                Priority = ConvertPriority(sharedStep.Fields["Microsoft.VSTS.Common.Priority"] as int? ?? 3),
                Attributes = new List<CaseAttribute>
                {
                    new()
                    {
                        Id = attributeMap[Constants.IterationAttributeName],
                        Value = GetValueOfField(sharedStep.Fields, "System.IterationPath")
                    }
                },
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

    private PriorityType ConvertPriority(int priority)
    {
        switch (priority)
        {
            case 1:
                return PriorityType.Highest;
            case 2:
                return PriorityType.High;
            case 3:
                return PriorityType.Medium;
            case 4:
                return PriorityType.Low;
            default:
                _logger.LogError("Failed to convert priority {Priority}", priority);

                throw new Exception($"Failed to convert priority {priority}");
        }
    }

    private static string GetValueOfField(IDictionary<string, object> fields, string key)
    {
        if (fields.TryGetValue(key, out var value))
        {
            return value as string ?? string.Empty;
        }

        return string.Empty;
    }
}
