using TestRailExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using TestRailExporter.Models;

namespace TestRailExporter.Services;

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

    public async Task<SharedStepInfo> ConvertSharedSteps(
        int projectId,
        Guid sectionId)
    {
        _logger.LogInformation("Converting shared steps");

        var testRailSharedSteps = await _client.GetSharedStepIdsByProjectId(projectId);

        _logger.LogDebug("Found {Count} shared steps", testRailSharedSteps.Count);

        var sharedSteps = new List<SharedStep>();
        var sharedStepsMap = new Dictionary<int, SharedStep>();

        foreach (var testRailSharedStep in testRailSharedSteps)
        {
            var sharedStepGuid = Guid.NewGuid();
            var stepsInfo = await _stepService.ConvertStepsForSharedStep(testRailSharedStep, sharedStepGuid);

            var sharedStep = new SharedStep
            {
                Id = sharedStepGuid,
                Name = testRailSharedStep.Title,
                Description = string.Empty,
                Steps = stepsInfo.Steps,
                State = StateType.NotReady,
                Priority = PriorityType.Medium,
                Attributes = new List<CaseAttribute>(),
                Links = new List<Link>(),
                Tags = new List<string>(),
                Attachments = stepsInfo.StepAttachmentNames,
                SectionId = sectionId
            };

            _logger.LogDebug("Converted shared step: {@SharedStep}", sharedStep);

            sharedSteps.Add(sharedStep);
            sharedStepsMap.Add(testRailSharedStep.Id, sharedStep);
        }

        return new SharedStepInfo {
            SharedSteps = sharedSteps,
            SharedStepsMap = sharedStepsMap
        };
    }
}
