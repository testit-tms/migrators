using QaseExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using QaseExporter.Models;

namespace QaseExporter.Services;

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

    public async Task<Dictionary<string, SharedStep>> ConvertSharedSteps(Guid sectionId)
    {
        _logger.LogInformation("Converting shared steps");

        var sharedSteps = await _client.GetSharedSteps();

        _logger.LogDebug("Found {Count} shared steps: {@SharedSteps}", sharedSteps.Count, sharedSteps);

        var tmsSharedSteps = new Dictionary<string, SharedStep>();

        foreach (var sharedStep in sharedSteps)
        {
            var sharedStepGuid = Guid.NewGuid();
            var attachments = new List<string>();
            var steps = await _stepService.ConvertSteps(sharedStep.Steps, tmsSharedSteps, sharedStepGuid);

            steps.ForEach(s =>
            {
                attachments.AddRange(s.ActionAttachments);
                attachments.AddRange(s.ExpectedAttachments);
                attachments.AddRange(s.TestDataAttachments);
            });

            var step = new SharedStep
            {
                Id = sharedStepGuid,
                Name = sharedStep.Name,
                Description = string.Empty,
                Steps = steps,
                State = StateType.NotReady,
                Priority = PriorityType.Medium,
                Attributes = new List<CaseAttribute>(),
                Links = new List<Link>(),
                Tags = new List<string>(),
                Attachments = attachments,
                SectionId = sectionId
            };

            _logger.LogDebug("Converted shared step: {@Step}", step);

            tmsSharedSteps.Add(sharedStep.Hash, step);
        }

        return tmsSharedSteps;
    }
}
