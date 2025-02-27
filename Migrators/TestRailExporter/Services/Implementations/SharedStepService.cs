using TestRailExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services.Implementations;

public class SharedStepService(
    ILogger<SharedStepService> logger,
    IClient client,
    IStepService stepService,
    IAttachmentService attachmentService)
    : ISharedStepService
{
    private readonly IAttachmentService _attachmentService = attachmentService;

    public async Task<SharedStepInfo> ConvertSharedSteps(
        int projectId,
        Guid sectionId)
    {
        logger.LogInformation("Converting shared steps");

        var testRailSharedSteps = await client.GetSharedStepIdsByProjectId(projectId);

        logger.LogDebug("Found {Count} shared steps", testRailSharedSteps.Count);

        var sharedSteps = new List<SharedStep>();
        var sharedStepsMap = new Dictionary<int, SharedStep>();

        foreach (var testRailSharedStep in testRailSharedSteps)
        {
            var sharedStepGuid = Guid.NewGuid();
            var stepsInfo = await stepService.ConvertStepsForSharedStep(testRailSharedStep, sharedStepGuid);

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

            logger.LogDebug("Converted shared step: {@SharedStep}", sharedStep);

            sharedSteps.Add(sharedStep);
            sharedStepsMap.Add(testRailSharedStep.Id, sharedStep);
        }

        return new SharedStepInfo
        {
            SharedSteps = sharedSteps,
            SharedStepsMap = sharedStepsMap
        };
    }
}
