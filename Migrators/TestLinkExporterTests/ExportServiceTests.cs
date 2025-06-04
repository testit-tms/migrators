using TestLinkExporter.Client;
using TestLinkExporter.Models.Project;
using TestLinkExporter.Models.Suite;
using TestLinkExporter.Services;
using TestLinkExporter.Services.Implementations;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Attribute = Models.Attribute;
using Constants = TestLinkExporter.Models.Project.Constants;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace TestLinkExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger;
    private IClient _client;
    private IWriteService _writeService;
    private ISectionService _sectionService;
    private ITestCaseService _testCaseService;
    private IAttributeService _attributeService;
    private TestLinkProject _project;
    private Attribute _idAttribute;
    private SectionData _sectionData;
    private readonly Guid _sectionId = Guid.NewGuid();
    private TestCase _testCase;
    private Root _mainJson;
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

        _project = new TestLinkProject
        {
            Id = 1,
            Name = "Project name"
        };

        _idAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.TestLinkId,
            IsActive = true,
            IsRequired = false,
            Type = AttributeType.String,
        };

        _sectionDictionary = new Dictionary<int, Guid>
        {
            { 1, _sectionId }
        };

        _sectionData = new SectionData
        {
            Sections = new List<Section> {
                new Section
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
                }
            },
            SectionMap = _sectionDictionary
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
            Attributes = new List<Attribute>{ _idAttribute },
            ProjectName = _project.Name,
            Sections = _sectionData.Sections,
            TestCases = new List<Guid> { _testCase.Id },
            SharedSteps = new List<Guid>()
        };
    }

    [Test]
    public async Task ExportProject_FailedGetProject()
    {
        // Arrange
        _client.GetProject().Throws(new Exception("Failed to get project"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService, _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        _sectionService.DidNotReceive().ConvertSections(Arg.Any<int>());
        await _testCaseService.DidNotReceive().ConvertTestCases(Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>());
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertSection()
    {
        // Arrange
        _client.GetProject().Returns(_project);
        _attributeService.GetCustomAttributes().Returns([_idAttribute]);
        _sectionService.ConvertSections(_project.Id).Throws(new Exception("Failed to get suites"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService, _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive().ConvertTestCases(Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>());
        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertTestCases()
    {
        // Arrange
        _client.GetProject().Returns(_project);
        _attributeService.GetCustomAttributes().Returns(new List<Attribute> { _idAttribute });
        _sectionService.ConvertSections(_project.Id).Returns(_sectionData);
        _testCaseService.ConvertTestCases(
            _sectionData.SectionMap,
            Arg.Any<Dictionary<string, Guid>>()).Throws(new Exception("Failed to get test cases"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService, _attributeService);

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
        _attributeService.GetCustomAttributes().Returns([_idAttribute]);
        _sectionService.ConvertSections(_project.Id).Returns(_sectionData);
        _testCaseService.ConvertTestCases(
            _sectionData.SectionMap, Arg.Any<Dictionary<string, Guid>>()).Returns(new List<TestCase>(){ _testCase });

        _writeService.WriteTestCase(_testCase).ThrowsAsync(new Exception("Failed to write test case"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService, _attributeService);

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
        _client.GetProject().Returns(_project);
        _attributeService.GetCustomAttributes().Returns([_idAttribute]);
        _sectionService.ConvertSections(_project.Id).Returns(_sectionData);
        _testCaseService.ConvertTestCases(
            _sectionData.SectionMap, Arg.Any<Dictionary<string, Guid>>()).Returns(new List<TestCase>(){ _testCase });

        _writeService.WriteMainJson(Arg.Is<Root>(r => _mainJson.Sections.SequenceEqual(r.Sections)
                                                      && _mainJson.Attributes.SequenceEqual(r.Attributes)
                                                      && _mainJson.SharedSteps.SequenceEqual(r.SharedSteps)
                                                      && _mainJson.TestCases.SequenceEqual(r.TestCases)
                                                      && _mainJson.ProjectName.Equals(r.ProjectName)))
            .ThrowsAsync(new Exception("Failed to write main json"));

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService, _attributeService);

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
        _client.GetProject().Returns(_project);
        _attributeService.GetCustomAttributes().Returns([_idAttribute]);
        _sectionService.ConvertSections(_project.Id).Returns(_sectionData);
        _testCaseService.ConvertTestCases(
            _sectionData.SectionMap, Arg.Any<Dictionary<string, Guid>>()).Returns([_testCase]);

        var service = new ExportService(_logger, _client, _writeService, _sectionService, _testCaseService, _attributeService);

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
