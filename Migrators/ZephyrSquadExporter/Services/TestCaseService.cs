using Microsoft.Extensions.Logging;
using Models;
using ZephyrSquadExporter.Client;
using ZephyrSquadExporter.Models;

namespace ZephyrSquadExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<TestCase>> ConvertTestCases(Dictionary<string, ZephyrSection> sectionMap)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();

        foreach (var section in sectionMap)
        {
            var executions = await GetExecution(section.Key, section.Value);

            foreach (var execution in executions)
            {
                var testCase = new TestCase()
                {
                    Id = Guid.NewGuid(),
                    Name = execution.IssueKey,
                    Description = execution.IssueDescription,
                    State = StateType.NotReady,
                    Priority = PriorityType.Medium,
                    Steps = new List<Step>(),
                    Tags = string.IsNullOrEmpty(execution.IssueLabel)
                        ? new List<string>()
                        : execution.IssueLabel.Split(",").ToList(),
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Duration = 10,
                    Attributes = new List<CaseAttribute>(),
                    Attachments = new List<string>(),
                    Iterations = new List<Iteration>(),
                    Links = new List<Link>(),
                    SectionId = section.Value.Guid
                };

                testCases.Add(testCase);
            }
        }

        return testCases;
    }

    private async Task<List<ZephyrExecution>> GetExecution(string sectionId, ZephyrSection section)
    {
        if (section.IsFolder)
        {
            return await _client.GetTestCasesFromFolder(section.CycleId, sectionId);
        }

        return await _client.GetTestCasesFromCycle(sectionId);
    }
}
