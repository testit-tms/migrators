using Microsoft.Extensions.Logging;
using Models;
using ZephyrSquadServerExporter.Client;
using ZephyrSquadServerExporter.Models;

namespace ZephyrSquadServerExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;
    public const int _duration = 10000;

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public async Task<List<TestCase>> ConvertTestCases(List<ZephyrSection> allSections)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();

        foreach (var section in allSections)
        {
            var executions = await GetExecution(section);

            foreach (var execution in executions)
            {
                var testCaseId = Guid.NewGuid();

                var issue = await _client.GetIssueById(execution.IssueId.ToString());

                var steps = await _stepService.ConvertSteps(testCaseId, execution.IssueId.ToString());

                var attachments = await _attachmentService.GetAttachmentsForIssue(testCaseId, issue.Fields.Attachments);

                steps.ForEach(s =>
                {
                    attachments.AddRange(s.ActionAttachments);
                    attachments.AddRange(s.ExpectedAttachments);
                    attachments.AddRange(s.TestDataAttachments);
                });

                var a = issue.Fields.Attachments;

                var testCase = new TestCase
                {
                    Id = testCaseId,
                    Name = execution.Name,
                    Description = execution.Description,
                    State = StateType.NotReady,
                    Priority = PriorityType.Medium,
                    Steps = steps,
                    Tags = issue.Fields.labels,
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Duration = _duration,
                    Attributes = new List<CaseAttribute>(),
                    Attachments = attachments,
                    Iterations = new List<Iteration>(),
                    Links = new List<Link>(),
                    SectionId = section.Id
                };

                testCases.Add(testCase);
            }
        }

        return testCases;
    }



    private async Task<List<ZephyrExecution>> GetExecution(ZephyrSection section)
    {
        if (section.FolderId != null)
        {
            return await _client.GetTestCasesFromFolder(section.ProjectId, section.VersionId, section.CycleId, section.FolderId);
        }

        return await _client.GetTestCasesFromCycle(section.ProjectId, section.VersionId, section.CycleId);
    }
}
