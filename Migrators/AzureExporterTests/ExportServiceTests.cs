using AzureExporter.Client;
using AzureExporter.Models;
using AzureExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Attribute = Models.Attribute;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace AzureExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger;
    private IClient _client;
    private IWriteService _writeService;
    private ITestCaseService _testCaseService;
    private ISharedStepService _sharedStepService;

    private Dictionary<int, SharedStep> _sharedSteps;
    private AzureProject _project;
    private Section _section;
    private TestCase _testCase;
    private Root _mainJson;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExportService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _sharedStepService = Substitute.For<ISharedStepService>();

        _project = new AzureProject
        {
            Id = Guid.NewGuid(),
            Name = "Project name"
        };

        _section = new Section
        {
            Name = "Azure DevOps",
            Id = Guid.NewGuid(),
            Sections = new List<Section>(),
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>()
        };

        _sharedSteps = new Dictionary<int, SharedStep>
        {
            {
                1, new SharedStep
                {
                    Name = "Shared Step 1",
                    Id = Guid.NewGuid(),
                    Steps = new List<Step>(),
                    Attributes = new List<CaseAttribute>(),
                    Description = "Description",
                    State = StateType.Ready,
                    Priority = PriorityType.Medium,
                    Attachments = new List<string>(),
                    Tags = new List<string>(),
                    Links = new List<Link>(),
                    SectionId = _section.Id
                }
            }
        };

        _testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            Name = "Test Case 1",
            Steps = new List<Step>(),
            SectionId = _section.Id,
            Attributes = new List<CaseAttribute>(),
            Description = "Description",
            State = StateType.Ready,
            Priority = PriorityType.Medium,
            Attachments = new List<string>(),
            Duration = 0,
            Tags = new List<string>(),
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Links = new List<Link>(),
            Iterations = new List<Iteration>()
        };

        _mainJson = new Root
        {
            Attributes = new List<Attribute>(),
            ProjectName = _project.Name,
            Sections = new List<Section> { _section },
            TestCases = new List<Guid> { _testCase.Id },
            SharedSteps = new List<Guid>()
        };
    }

    [Test]
    public async Task ExportProject_FailedGetProject()
    {
        // Arrange
        _client.GetProject().ThrowsAsync(new Exception("Failed to get project"));

        var service = new ExportService(_logger, _client, _testCaseService, _writeService, _sharedStepService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _sharedStepService.DidNotReceive().ConvertSharedSteps(Arg.Any<Guid>(), Arg.Any<Guid>());
        await _testCaseService.DidNotReceive().ConvertTestCases(Arg.Any<Guid>(), Arg.Any<Dictionary<int, Guid>>(),
            Arg.Any<Guid>());
        await _writeService.DidNotReceive().WriteSharedStep(Arg.Any<SharedStep>());
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertSharedSteps()
    {
        // Arrange
        _client.GetProject().Returns(_project);
        _sharedStepService.ConvertSharedSteps(_project.Id, Arg.Any<Guid>())
            .ThrowsAsync(new Exception("Failed to get shared steps"));

        var service = new ExportService(_logger, _client, _testCaseService, _writeService, _sharedStepService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive().ConvertTestCases(Arg.Any<Guid>(), Arg.Any<Dictionary<int, Guid>>(),
            Arg.Any<Guid>());
        await _writeService.DidNotReceive().WriteSharedStep(Arg.Any<SharedStep>());
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertTestCases()
    {
        // Arrange
        _client.GetProject().Returns(_project);
        _sharedStepService.ConvertSharedSteps(_project.Id, Arg.Any<Guid>()).Returns(_sharedSteps);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<int, Guid>>(),
            Arg.Any<Guid>()).ThrowsAsync(new Exception("Failed to get attributes"));

        var service = new ExportService(_logger, _client, _testCaseService, _writeService, _sharedStepService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.DidNotReceive().WriteSharedStep(Arg.Any<SharedStep>());
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteSharedStep()
    {
        // Arrange
        _client.GetProject().Returns(_project);
        _sharedStepService.ConvertSharedSteps(_project.Id, Arg.Any<Guid>()).Returns(_sharedSteps);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<int, Guid>>(),
            Arg.Any<Guid>()).Returns(new List<TestCase> { _testCase });
        _writeService.WriteSharedStep(_sharedSteps[1]).ThrowsAsync(new Exception("Failed to write shared step"));

        var service = new ExportService(_logger, _client, _testCaseService, _writeService, _sharedStepService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteTestCase()
    {
        // Arrange
        _client.GetProject().Returns(_project);
        _sharedStepService.ConvertSharedSteps(_project.Id, Arg.Any<Guid>()).Returns(_sharedSteps);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<int, Guid>>(),
            Arg.Any<Guid>()).Returns(new List<TestCase> { _testCase });

        _writeService.WriteTestCase(_testCase).ThrowsAsync(new Exception("Failed to write test case"));

        var service = new ExportService(_logger, _client, _testCaseService, _writeService, _sharedStepService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.Received().WriteSharedStep(Arg.Any<SharedStep>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteMainJson()
    {
        // Arrange
        _client.GetProject().Returns(_project);
        _sharedStepService.ConvertSharedSteps(_project.Id, Arg.Any<Guid>()).Returns(_sharedSteps);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<int, Guid>>(),
            Arg.Any<Guid>()).Returns(new List<TestCase> { _testCase });

        _writeService.WriteMainJson(Arg.Any<Root>())
            .ThrowsAsync(new Exception("Failed to write main json"));

        var service = new ExportService(_logger, _client, _testCaseService, _writeService, _sharedStepService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.Received().WriteSharedStep(Arg.Any<SharedStep>());
        await _writeService.Received().WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        _client.GetProject().Returns(_project);
        _sharedStepService.ConvertSharedSteps(_project.Id, Arg.Any<Guid>()).Returns(_sharedSteps);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<int, Guid>>(),
            Arg.Any<Guid>()).Returns(new List<TestCase> { _testCase });


        var service = new ExportService(_logger, _client, _testCaseService, _writeService, _sharedStepService);

        // Act
        await service.ExportProject();

        // Assert
        await _writeService.Received().WriteTestCase(_testCase);
        await _writeService.Received().WriteMainJson(Arg.Any<Root>());
    }
}
