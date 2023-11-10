using Microsoft.Extensions.Logging;
using Models;
using TestCollabExporter.Client;
using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public class SharedStepService : ISharedStepService
{
    private readonly ILogger<SharedStepService> _logger;
    private readonly IClient _client;

    public SharedStepService(ILogger<SharedStepService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<SharedStepData> ConvertSharedSteps(int projectId, Guid sectionId, List<Guid> attributes)
    {
        _logger.LogInformation("Getting shared steps");

        var testCollabSharedSteps = await _client.GetSharedSteps(projectId);

        var sharedSteps = new List<SharedStep>(testCollabSharedSteps.Count);
        var sharedStepMap = new Dictionary<int, Guid>(testCollabSharedSteps.Count);

        foreach (var testCollabSharedStep in testCollabSharedSteps)
        {
            var step = new SharedStep
            {
                Id = Guid.NewGuid(),
                Name = testCollabSharedStep.Name,
                State = StateType.NeedsWork,
                Priority = PriorityType.Medium,
                Attributes = attributes.Select(a =>
                        new CaseAttribute
                        {
                            Id = a,
                            Value = string.Empty
                        })
                    .ToList(),
                Attachments = new List<string>(),
                Links = new List<Link>(),
                SectionId = sectionId,
                Tags = new List<string>(),
                Description = string.Empty,
                Steps = testCollabSharedStep.Steps.Select(s => new Step()
                {
                    Action = s.Step,
                    Expected = s.ExpectedResult,
                    TestData = string.Empty,
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>()
                }).ToList()
            };

            sharedSteps.Add(step);
            sharedStepMap.Add(testCollabSharedStep.Id, step.Id);
        }

        return new SharedStepData
        {
            SharedStepsMap = sharedStepMap,
            SharedSteps = sharedSteps
        };
    }
}
