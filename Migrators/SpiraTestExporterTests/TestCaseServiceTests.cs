using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpiraTestExporter.Client;
using SpiraTestExporter.Models;
using SpiraTestExporter.Services;
using Constants = SpiraTestExporter.Models.Constants;

namespace SpiraTestExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;

    private const int ProjectId = 1;

    private Dictionary<int, Guid> _sectionMap;
    private Dictionary<int, string> _priorities;
    private Dictionary<int, string> _statuses;
    private Dictionary<string, Guid> _attributesMap;

    private List<SpiraTest> _testCases;
    private List<string> _attachments;
    private List<SpiraStep> _spiraSteps;
    private List<SpiraStepParameter> _spiraStepParameters;
    private List<SpiraTestCaseParameter> _spiraParameters;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _attachmentService = Substitute.For<IAttachmentService>();

        _sectionMap = new Dictionary<int, Guid>
        {
            { 2, Guid.NewGuid() }
        };

        _priorities = new Dictionary<int, string>
        {
            { 1, "High" }
        };

        _statuses = new Dictionary<int, string>
        {
            { 1, "Passed" }
        };

        _attributesMap = new Dictionary<string, Guid>
        {
            { Constants.Priority, Guid.NewGuid() },
            { Constants.Status, Guid.NewGuid() }
        };

        _testCases = new List<SpiraTest>
        {
            new()
            {
                TestCaseId = 1,
                Name = "Test Case 1",
                Description = "Test Case 1 Description",
                AuthorName = "Test User 1",
                PriorityId = 1,
                StatusId = 1,
                FolderId = 2
            }
        };

        _attachments = new List<string>
        {
            "attachment1.txt",
            "attachment2.txt"
        };

        _spiraSteps = new List<SpiraStep>
        {
            new()
            {
                Id = 123,
                Description = "Step 1",
                ExpectedResult = "Expected Result 1",
                Position = 1
            }
        };

        _spiraStepParameters = new List<SpiraStepParameter>
        {
            new()
            {
                Name = "Parameter 1",
                Value = "Value 1"
            }
        };

        _spiraParameters = new List<SpiraTestCaseParameter>
        {
            new()
            {
                Name = "Parameter 1",
                Value = "Value 1"
            }
        };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestFromFolder()
    {
        // Arrange
        _client.GetTestFromFolder(ProjectId, _sectionMap.First().Key)
            .Throws(new Exception("Failed to get test from folder"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _priorities, _statuses, _attributesMap));

        // Assert
        await _attachmentService.DidNotReceive()
            .GetAttachments(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<ArtifactType>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetTestSteps(Arg.Any<int>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetStepParameters(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetSpiraParameters(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetAttachments()
    {
        // Arrange
        _client.GetTestFromFolder(ProjectId, _sectionMap.First().Key)
            .Returns(_testCases);

        _attachmentService
            .GetAttachments(Arg.Any<Guid>(), ProjectId, ArtifactType.TestCase, _testCases.First().TestCaseId)
            .Throws(new Exception("Failed to get attachments"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _priorities, _statuses, _attributesMap));

        // Assert
        await _client.DidNotReceive()
            .GetTestSteps(Arg.Any<int>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetStepParameters(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetSpiraParameters(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestSteps()
    {
        // Arrange
        _client.GetTestFromFolder(ProjectId, _sectionMap.First().Key)
            .Returns(_testCases);

        _attachmentService
            .GetAttachments(Arg.Any<Guid>(), ProjectId, ArtifactType.TestCase, _testCases.First().TestCaseId)
            .Returns(_attachments);

        _client.GetTestSteps(ProjectId, _testCases.First().TestCaseId)
            .Throws(new Exception("Failed to get test steps"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _priorities, _statuses, _attributesMap));

        // Assert
        await _client.DidNotReceive()
            .GetStepParameters(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetSpiraParameters(Arg.Any<int>(), Arg.Any<int>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetStepParameters()
    {
        // Arrange
        _client.GetTestFromFolder(ProjectId, _sectionMap.First().Key)
            .Returns(_testCases);

        _attachmentService
            .GetAttachments(Arg.Any<Guid>(), ProjectId, ArtifactType.TestCase, _testCases.First().TestCaseId)
            .Returns(_attachments);

        _client.GetTestSteps(ProjectId, _testCases.First().TestCaseId)
            .Returns(_spiraSteps);

        _client.GetStepParameters(ProjectId, _testCases.First().TestCaseId, _spiraSteps.First().Id)
            .Throws(new Exception("Failed to get step parameters"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _priorities, _statuses, _attributesMap));

        // Assert
        await _client.DidNotReceive()
            .GetSpiraParameters(Arg.Any<int>(), Arg.Any<int>());
    }


    [Test]
    public async Task ConvertTestCases_FailedGetSpiraParameters()
    {
        // Arrange
        _client.GetTestFromFolder(ProjectId, _sectionMap.First().Key)
            .Returns(_testCases);

        _attachmentService
            .GetAttachments(Arg.Any<Guid>(), ProjectId, ArtifactType.TestCase, _testCases.First().TestCaseId)
            .Returns(_attachments);

        _client.GetTestSteps(ProjectId, _testCases.First().TestCaseId)
            .Returns(_spiraSteps);

        _client.GetStepParameters(ProjectId, _testCases.First().TestCaseId, _spiraSteps.First().Id)
            .Returns(_spiraStepParameters);

        _attachmentService
            .GetAttachments(Arg.Any<Guid>(), ProjectId, ArtifactType.Step, _spiraSteps.First().Id)
            .Returns(_attachments);

        _client.GetSpiraParameters(ProjectId, _testCases.First().TestCaseId)
            .Throws(new Exception("Failed to get Spira parameters"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _priorities, _statuses, _attributesMap));
    }

    [Test]
    public async Task ConvertTestCases_Success()
    {
        // Arrange
        _client.GetTestFromFolder(ProjectId, _sectionMap.First().Key)
            .Returns(_testCases);

        _attachmentService
            .GetAttachments(Arg.Any<Guid>(), ProjectId, ArtifactType.TestCase, _testCases.First().TestCaseId)
            .Returns(_attachments);

        _client.GetTestSteps(ProjectId, _testCases.First().TestCaseId)
            .Returns(_spiraSteps);

        _client.GetStepParameters(ProjectId, _testCases.First().TestCaseId, _spiraSteps.First().Id)
            .Returns(_spiraStepParameters);

        _attachmentService
            .GetAttachments(Arg.Any<Guid>(), ProjectId, ArtifactType.Step, _spiraSteps.First().Id)
            .Returns(_attachments);

        _client.GetSpiraParameters(ProjectId, _testCases.First().TestCaseId)
            .Returns(_spiraParameters);

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        var result =
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _priorities, _statuses, _attributesMap);

        // Assert
        Assert.That(result.TestCases, Has.Count.EqualTo(1));
        Assert.That(result.TestCases.First().Name, Is.EqualTo(_testCases.First().Name));
        Assert.That(result.TestCases.First().Description, Is.EqualTo(_testCases.First().Description));
        Assert.That(result.TestCases.First().Priority, Is.EqualTo(PriorityType.Medium));
        Assert.That(result.TestCases.First().State, Is.EqualTo(StateType.NotReady));
        Assert.That(result.TestCases.First().SectionId, Is.EqualTo(_sectionMap.First().Value));
        Assert.That(result.TestCases.First().Steps, Has.Count.EqualTo(1));
        Assert.That(result.TestCases.First().Steps.First().Action, Is.EqualTo(_spiraSteps.First().Description));
        Assert.That(result.TestCases.First().Steps.First().Expected, Is.EqualTo(_spiraSteps.First().ExpectedResult));
        Assert.That(result.TestCases.First().Steps.First().TestDataAttachments, Has.Count.EqualTo(4));
        Assert.That(result.TestCases.First().Steps.First().TestDataAttachments.First(),
            Is.EqualTo(_attachments.First()));
        Assert.That(result.TestCases.First().Steps.First().TestDataAttachments.Last(), Is.EqualTo(_attachments.Last()));
        Assert.That(result.TestCases.First().PreconditionSteps, Has.Count.EqualTo(0));
        Assert.That(result.TestCases.First().Iterations, Has.Count.EqualTo(1));
        Assert.That(result.TestCases.First().Iterations.First().Parameters, Has.Count.EqualTo(1));
        Assert.That(result.TestCases.First().Iterations.First().Parameters.First().Name,
            Is.EqualTo(_spiraParameters.First().Name));
        Assert.That(result.TestCases.First().Iterations.First().Parameters.First().Value,
            Is.EqualTo(_spiraParameters.First().Value));
    }
}
