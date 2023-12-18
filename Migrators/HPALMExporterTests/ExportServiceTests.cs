using HPALMExporter.Client;
using HPALMExporter.Models;
using HPALMExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Attribute = Models.Attribute;
using TestCaseData = HPALMExporter.Models.TestCaseData;

namespace HPALMExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger;
    private IClient _client;
    private IAttributeService _attributeService;
    private ISectionService _sectionService;
    private ITestCaseService _testCaseService;
    private IWriteService _writeService;

    private List<Attribute> _attributes;
    private SectionData _sections;
    private TestCaseData _testCases;
    private Root _root;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExportService>>();
        _client = Substitute.For<IClient>();
        _attributeService = Substitute.For<IAttributeService>();
        _sectionService = Substitute.For<ISectionService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _writeService = Substitute.For<IWriteService>();

        _attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Attribute1",
                Type = AttributeType.String,
                IsRequired = false,
                IsActive = true,
                Options = new List<string>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Attribute2",
                Type = AttributeType.Options,
                IsRequired = false,
                IsActive = true,
                Options = new List<string>
                {
                    "Option1",
                    "Option2"
                }
            }
        };

        var sectionGuid = Guid.NewGuid();

        _sections = new SectionData
        {
            SectionMap = new Dictionary<int, Guid>
            {
                { 1, sectionGuid }
            },
            Sections = new List<Section>
            {
                new()
                {
                    Id = sectionGuid,
                    Name = "Section1",
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Sections = new List<Section>()
                }
            }
        };

        _testCases = new TestCaseData
        {
            SharedSteps = new List<SharedStep>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "SharedStep1"
                }
            },
            TestCases = new List<TestCase>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase1"
                }
            }
        };

        _root = new Root
        {
            ProjectName = "Project1",
            Sections = _sections.Sections,
            Attributes = _attributes,
            SharedSteps = _testCases.SharedSteps.Select(s => s.Id).ToList(),
            TestCases = _testCases.TestCases.Select(t => t.Id).ToList()
        };
    }

    [Test]
    public async Task ExportProject_FailedAuth()
    {
        // Arrange
        _client.Auth()
            .Throws(new Exception("Failed to auth"));

        var exportService = new ExportService(_logger, _client, _attributeService, _sectionService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        _client.DidNotReceive()
            .GetProjectName();

        await _attributeService.DidNotReceive()
            .ConvertAttributes();

        await _sectionService.DidNotReceive()
            .ConvertSections();

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertAttributes()
    {
        // Arrange
        _client.Auth()
            .Returns(Task.CompletedTask);

        _attributeService.ConvertAttributes()
            .Throws(new Exception("Failed to convert attributes"));

        var exportService = new ExportService(_logger, _client, _attributeService, _sectionService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _sectionService.DidNotReceive()
            .ConvertSections();

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        _client.DidNotReceive()
            .GetProjectName();
    }

    [Test]
    public async Task ExportProject_FailedConvertSections()
    {
        // Arrange
        _client.Auth()
            .Returns(Task.CompletedTask);

        _attributeService.ConvertAttributes()
            .Returns(_attributes);

        _sectionService.ConvertSections()
            .Throws(new Exception("Failed to convert sections"));

        var exportService = new ExportService(_logger, _client, _attributeService, _sectionService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        _client.DidNotReceive()
            .GetProjectName();
    }

    [Test]
    public async Task ExportProject_FailedConvertTestCases()
    {
        // Arrange
        _client.Auth()
            .Returns(Task.CompletedTask);

        _attributeService.ConvertAttributes()
            .Returns(_attributes);

        _sectionService.ConvertSections()
            .Returns(_sections);

        var attributeMap = _attributes.ToDictionary(a => a.Name, a => a.Id);

        _testCaseService.ConvertTestCases(
                Arg.Is<Dictionary<int, Guid>>(s => s.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(s => s.SequenceEqual(attributeMap)))
            .Throws(new Exception("Failed to convert test cases"));

        var exportService = new ExportService(_logger, _client, _attributeService, _sectionService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        _client.DidNotReceive()
            .GetProjectName();
    }

    [Test]
    public async Task ExportProject_FailedWriteSharedStep()
    {
        // Arrange
        _client.Auth()
            .Returns(Task.CompletedTask);

        _attributeService.ConvertAttributes()
            .Returns(_attributes);

        _sectionService.ConvertSections()
            .Returns(_sections);

        var attributeMap = _attributes.ToDictionary(a => a.Name, a => a.Id);

        _testCaseService.ConvertTestCases(
                Arg.Is<Dictionary<int, Guid>>(s => s.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(s => s.SequenceEqual(attributeMap)))
            .Returns(_testCases);

        _writeService.WriteSharedStep(_testCases.SharedSteps.First())
            .Throws(new Exception("Failed to write shared step"));

        var exportService = new ExportService(_logger, _client, _attributeService, _sectionService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        _client.DidNotReceive()
            .GetProjectName();
    }

    [Test]
    public async Task ExportProject_FailedWriteTestCase()
    {
        // Arrange
        _client.Auth()
            .Returns(Task.CompletedTask);

        _attributeService.ConvertAttributes()
            .Returns(_attributes);

        _sectionService.ConvertSections()
            .Returns(_sections);

        var attributeMap = _attributes.ToDictionary(a => a.Name, a => a.Id);

        _testCaseService.ConvertTestCases(
                Arg.Is<Dictionary<int, Guid>>(s => s.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(s => s.SequenceEqual(attributeMap)))
            .Returns(_testCases);

        _writeService.WriteTestCase(_testCases.TestCases.First())
            .Throws(new Exception("Failed to write test case"));

        var exportService = new ExportService(_logger, _client, _attributeService, _sectionService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.Received(_testCases.SharedSteps.Count)
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        _client.DidNotReceive()
            .GetProjectName();
    }

    [Test]
    public async Task ExportProject_FailedWriteMainJson()
    {
        // Arrange
        _client.Auth()
            .Returns(Task.CompletedTask);

        _attributeService.ConvertAttributes()
            .Returns(_attributes);

        _sectionService.ConvertSections()
            .Returns(_sections);

        var attributeMap = _attributes.ToDictionary(a => a.Name, a => a.Id);

        _testCaseService.ConvertTestCases(
                Arg.Is<Dictionary<int, Guid>>(s => s.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(s => s.SequenceEqual(attributeMap)))
            .Returns(_testCases);

        _client.GetProjectName()
            .Returns(_root.ProjectName);

        _writeService.WriteMainJson(
                Arg.Is<Root>(r => r.ProjectName == _root.ProjectName &&
                                  r.Sections.SequenceEqual(_root.Sections) &&
                                  r.Attributes.SequenceEqual(_root.Attributes) &&
                                  r.SharedSteps.SequenceEqual(_root.SharedSteps) &&
                                  r.TestCases.SequenceEqual(_root.TestCases)))
            .Throws(new Exception("Failed to write main json"));

        var exportService = new ExportService(_logger, _client, _attributeService, _sectionService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.Received(_testCases.SharedSteps.Count)
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.Received(_testCases.TestCases.Count)
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        _client.Auth()
            .Returns(Task.CompletedTask);

        _attributeService.ConvertAttributes()
            .Returns(_attributes);

        _sectionService.ConvertSections()
            .Returns(_sections);

        var attributeMap = _attributes.ToDictionary(a => a.Name, a => a.Id);

        _testCaseService.ConvertTestCases(
                Arg.Is<Dictionary<int, Guid>>(s => s.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(s => s.SequenceEqual(attributeMap)))
            .Returns(_testCases);

        _client.GetProjectName()
            .Returns(_root.ProjectName);

        var exportService = new ExportService(_logger, _client, _attributeService, _sectionService, _testCaseService,
            _writeService);

        // Act
         await exportService.ExportProject();

        // Assert
        await _writeService.Received(_testCases.SharedSteps.Count)
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.Received(_testCases.TestCases.Count)
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.Received()
            .WriteMainJson(Arg.Any<Root>());
    }
}
