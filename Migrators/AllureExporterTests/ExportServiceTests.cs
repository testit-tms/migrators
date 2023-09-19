using AllureExporter.Client;
using AllureExporter.Models;
using AllureExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Attribute = Models.Attribute;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace AllureExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger;
    private IClient _client;
    private IWriteService _writeService;
    private ISectionService _sectionService;
    private ITestCaseService _testCaseService;
    private IAttributeService _attributeService;
    private BaseEntity _project;
    private SectionInfo _sectionInfo;
    private readonly Guid _sectionId = Guid.NewGuid();
    private TestCase _testCase;
    private Root _mainJson;
    private List<Attribute> _attributes;
    private Dictionary<int, Guid> _sectionDictionary;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExportService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
        _sectionService = Substitute.For<ISectionService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _attributeService = Substitute.For<IAttributeService>();

        _project = new BaseEntity
        {
            Id = 1,
            Name = "Project name"
        };

        _sectionDictionary = new Dictionary<int, Guid>
        {
            { 1, _sectionId }
        };

        _sectionInfo = new SectionInfo
        {
            MainSection = new Section
            {
                Id = _sectionId,
                Name = "Main Section",
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = new List<Section>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Child Section",
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>()
                    }
                }
            },
            SectionDictionary = _sectionDictionary
        };

        _attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Attribute 1",
                IsActive = true,
                IsRequired = false,
                Type = AttributeType.String,
                Options = new List<string>()
            }
        };

        _testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            Name = "Test Case 1",
            Steps = new List<Step>(),
            Attributes = new List<CaseAttribute>(),
            SectionId = _sectionId,
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
            Attributes = _attributes,
            ProjectName = _project.Name,
            Sections = new List<Section> { _sectionInfo.MainSection },
            TestCases = new List<Guid> { _testCase.Id },
            SharedSteps = new List<Guid>()
        };
    }

    [Test]
    public async Task ExportProject_FailedGetProject()
    {
        // Arrange
        _client.GetProjectId().ThrowsAsync(new Exception("Failed to get project"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService,
            _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _sectionService.DidNotReceive().ConvertSection(Arg.Any<int>());
        await _attributeService.DidNotReceive().GetCustomAttributes(Arg.Any<int>());
        await _testCaseService.DidNotReceive().ConvertTestCases(Arg.Any<int>(), Arg.Any<Dictionary<string, Guid>>(),
            Arg.Any<Dictionary<int, Guid>>());
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertSection()
    {
        // Arrange
        _client.GetProjectId().Returns(_project);
        _sectionService.ConvertSection(_project.Id).ThrowsAsync(new Exception("Failed to get suites"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService,
            _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _attributeService.DidNotReceive().GetCustomAttributes(Arg.Any<int>());
        await _testCaseService.DidNotReceive().ConvertTestCases(Arg.Any<int>(), Arg.Any<Dictionary<string, Guid>>(),
            Arg.Any<Dictionary<int, Guid>>());
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedGetCustomAttributes()
    {
        // Arrange
        _client.GetProjectId().Returns(_project);
        _sectionService.ConvertSection(_project.Id).Returns(_sectionInfo);
        _attributeService.GetCustomAttributes(_project.Id).ThrowsAsync(new Exception("Failed to get attributes"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService,
            _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive().ConvertTestCases(Arg.Any<int>(), Arg.Any<Dictionary<string, Guid>>(),
            Arg.Any<Dictionary<int, Guid>>());
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertTestCases()
    {
        // Arrange
        _client.GetProjectId().Returns(_project);
        _sectionService.ConvertSection(_project.Id).Returns(_sectionInfo);
        _attributeService.GetCustomAttributes(_project.Id).Returns(_attributes);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<string, Guid>>(),
            _sectionDictionary).ThrowsAsync(new Exception("Failed to get test cases"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService,
            _attributeService);

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
        _client.GetProjectId().Returns(_project);
        _sectionService.ConvertSection(_project.Id).Returns(_sectionInfo);
        _attributeService.GetCustomAttributes(_project.Id).Returns(_attributes);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<string, Guid>>(),
            _sectionDictionary).Returns(new List<TestCase>()
        {
            _testCase
        });

        _writeService.WriteTestCase(_testCase).ThrowsAsync(new Exception("Failed to write test case"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService,
            _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteMainJson()
    {
        // Arrange
        _client.GetProjectId().Returns(_project);
        _sectionService.ConvertSection(_project.Id).Returns(_sectionInfo);
        _attributeService.GetCustomAttributes(_project.Id).Returns(_attributes);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<string, Guid>>(),
            _sectionDictionary).Returns(new List<TestCase>()
        {
            _testCase
        });

        _writeService.WriteMainJson(Arg.Is<Root>(r => _mainJson.Sections.SequenceEqual(r.Sections)
                                                      && _mainJson.Attributes.SequenceEqual(r.Attributes)
                                                      && _mainJson.SharedSteps.SequenceEqual(r.SharedSteps)
                                                      && _mainJson.TestCases.SequenceEqual(r.TestCases)
                                                      && _mainJson.ProjectName.Equals(r.ProjectName)))
            .ThrowsAsync(new Exception("Failed to write main json"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService,
            _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.Received().WriteTestCase(_testCase);
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        _client.GetProjectId().Returns(_project);
        _sectionService.ConvertSection(_project.Id).Returns(_sectionInfo);
        _attributeService.GetCustomAttributes(_project.Id).Returns(_attributes);
        _testCaseService.ConvertTestCases(_project.Id, Arg.Any<Dictionary<string, Guid>>(),
            _sectionDictionary).Returns(new List<TestCase>()
        {
            _testCase
        });


        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService,
            _attributeService);

        // Act
        await service.ExportProject();

        // Assert
        await _writeService.Received().WriteTestCase(_testCase);
        await _writeService.Received().WriteMainJson(Arg.Is<Root>(r =>
            _mainJson.Sections.SequenceEqual(r.Sections)
            && _mainJson.Attributes.SequenceEqual(r.Attributes)
            && _mainJson.SharedSteps.SequenceEqual(r.SharedSteps)
            && _mainJson.TestCases.SequenceEqual(r.TestCases)
            && _mainJson.ProjectName.Equals(r.ProjectName)
        ));
    }
}
