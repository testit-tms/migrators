using AllureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;

namespace AllureExporter.Services;

public class SharedStepService : ISharedStepService
{
    private readonly ILogger<SharedStepService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;

    public SharedStepService(
        ILogger<SharedStepService> logger,
        IClient client,
        IStepService stepService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public async Task<Dictionary<long, SharedStep>> ConvertSharedSteps(
        long projectId,
        Guid sectionId,
        List<Attribute> attributes)
    {
        _logger.LogInformation("Converting shared steps");

        var sharedSteps = await _client.GetSharedStepsByProjectId(projectId);

        _logger.LogDebug("Found {Count} shared steps: {@SharedSteps}", sharedSteps.Count, sharedSteps);

        var tmsSharedSteps = new Dictionary<long, SharedStep>();

        foreach (var sharedStep in sharedSteps)
        {
            var sharedStepInfo = await _client.GetStepsInfoBySharedStepId(sharedStep.Id);

            _logger.LogDebug("Found shared step info by id {SharedStepId}: {@sharedStepInfo}", sharedStep.Id, sharedStepInfo);

            var steps = await _stepService.ConvertStepsForSharedStep(sharedStep.Id);

            _logger.LogDebug("Found {@Steps} steps", steps.Count);

            var sharedStepGuid = Guid.NewGuid();
            var tmsAttachments = await _attachmentService.DownloadAttachmentsforSharedStep(sharedStep.Id, sharedStepGuid);

            var step = new SharedStep
            {
                Id = sharedStepGuid,
                Name = sharedStep.Name,
                Description = string.Empty,
                Steps = steps,
                State = StateType.NotReady,
                Priority = PriorityType.Medium,
                Attributes = attributes.Select(a =>
                        new CaseAttribute
                        {
                            Id = a.Id,
                            Value = string.Empty
                        })
                    .ToList(),
                Links = new List<Link>(),
                Tags = new List<string>(),
                Attachments = tmsAttachments,
                SectionId = sectionId
            };

            _logger.LogDebug("Converted shared step: {@Step}", step);

            tmsSharedSteps.Add(sharedStep.Id, step);
        }

        return tmsSharedSteps;
    }
}
