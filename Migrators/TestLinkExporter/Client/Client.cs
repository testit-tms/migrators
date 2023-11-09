using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestLinkApi;
using TestLinkExporter.Models;

namespace TestLinkExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly TestLink _client;
    private readonly string _projectName;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("testLink");
        var url = section["url"];

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var token = section["token"];

        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Private token is not specified");
        }

        var projectName = section["projectName"];

        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("Project name is not specified");
        }

        _projectName = projectName;

        _client = new TestLink(token, url);
    }

    public TestLinkProject GetProject()
    {
        var project = _client.GetProject(_projectName);

        if (project == null)
        {
            _logger.LogError("Project {ProjectName} is not found", _projectName);

            throw new Exception($"Project {_projectName} is not found");
        }

        _logger.LogDebug("Received project by name {ProjectName}: {@Project}", _projectName, project);

        return new TestLinkProject
        {
            Id = project.id,
            Name = project.name,
        };
    }

    public List<TestLinkSuite> GetSuitesByProjectId(int id)
    {
        var suites = new List<TestLinkSuite>();

        var testSuites = _client.GetFirstLevelTestSuitesForTestProject(id);

        if (!testSuites.Any())
        {
            _logger.LogError("Test suites from {ProjectName} is not found", _projectName);

            throw new Exception($"Test suites from {_projectName} is not found");
        }

        _logger.LogDebug("Received test suites by id {Id}: {@TestSuites}", id, testSuites);

        foreach ( var suite in testSuites) {
            suites.Add(new TestLinkSuite
            {
                Id = suite.id,
                Name = suite.name,
                ParentId = suite.parentId
            });
        }

        return suites;
    }

    public List<TestLinkSuite> GetSharedSuitesBySuiteId(int id)
    {
        var suites = new List<TestLinkSuite>();

        var testSuites = _client.GetTestSuitesForTestSuite(id);

        _logger.LogDebug("Received shared test suites by test suite id {Id}: {@TestSuites}", id, testSuites);

        foreach (var suite in testSuites)
        {
            suites.Add(new TestLinkSuite
            {
                Id = suite.id,
                Name = suite.name,
                ParentId = suite.parentId
            });
        }

        return suites;
    }

    public List<int> GetTestCaseIdsBySuiteId(int id)
    {
        var testCaseIds = _client.GetTestCaseIdsForTestSuite(id, false);

        _logger.LogDebug("Received test case ids by test suite id {Id}: {@TestCaseIds}", id, testCaseIds);

        return testCaseIds;
    }

    public TestLinkTestCase GetTestCaseById(int id)
    {
        var testCase = _client.GetTestCase(id);

        if (testCase == null)
        {
            _logger.LogError("Test case with id {Id} is not found", id);

            throw new Exception($"Test case with id {id} is not found");
        }

        _logger.LogDebug("Received test case by id {Id}: {@TestCase}", id, testCase);

        return new TestLinkTestCase
        {
            Id = id,
            Name = testCase.name,
            ExternalId = testCase.externalid,
            ExecutionType = testCase.execution_type,
            Importance = testCase.importance,
            Preconditions = testCase.preconditions,
            Steps = testCase.steps.Select(step =>
                new TestLinkStep
                {
                    Actions = step.actions,
                    ExpectedResult = step.expected_results,
                    StepNumber = step.step_number,
                }
            )
            .ToList(),
            Summary = testCase.summary,
            TestSuiteId = testCase.testsuite_id,
            Status = testCase.status,
            Layout = testCase.layout,
            IsOpen = testCase.is_open
        };
    }

    public List<TestLinkAttachment> GetAttachmentsByTestCaseId(int id)
    {
        var attachments = _client.GetTestCaseAttachments(id);

        _logger.LogDebug("Received attachments by test case id {Id}: {@Attachments}", id, attachments);

        return attachments.Select(attachment =>
            new TestLinkAttachment
            {
                Content = attachment.content,
                FileType = attachment.file_type,
                Name = attachment.name,
            }
        )
        .ToList();
    }
}
