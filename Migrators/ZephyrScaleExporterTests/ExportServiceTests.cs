using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;
using ZephyrScaleExporter.Services;
using Attribute = Models.Attribute;
using TestCaseData = ZephyrScaleExporter.Models.TestCaseData;

namespace ZephyrScaleExporterTests;

public class ExportServiceTests
{
    private IClient _client;
    private IFolderService _folderService;
    private IAttributeService _attributeService;
    private ITestCaseService _testCaseService;
    private IWriteService _writeService;
    private ILogger<ExportService> _logger;
    private List<Section> _sections;
    private Dictionary<int, Guid> _sectionMap;
    private SectionData _sectionData;
    private ZephyrProject _project;
    private List<Attribute> _attributes;
    private Dictionary<string, Guid> _attributeMap;
    private Dictionary<int, string> _stateMap;
    private Dictionary<int, string> _priorityMap;
    private AttributeData _attributeData;
    private List<TestCase> _testCases;
    private TestCaseData _testCaseData;

    [SetUp]
    public void Setup()
    {
        _client = Substitute.For<IClient>();
        _folderService = Substitute.For<IFolderService>();
        _attributeService = Substitute.For<IAttributeService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _writeService = Substitute.For<IWriteService>();
        _logger = Substitute.For<ILogger<ExportService>>();

        _project = new ZephyrProject
        {
            Key = "Test"
        };

        _sections = new List<Section>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Section 1",
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Section 2",
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>()
            }
        };

        _sectionMap = new Dictionary<int, Guid>
        {
            {1, _sections[0].Id},
            {2, _sections[1].Id}
        };

        _sectionData = new SectionData
        {
            SectionMap = _sectionMap,
            Sections = _sections
        };

        _attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Attribute 1",
                Type = AttributeType.Options,
                Options = new List<string>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Attribute 2",
                Type = AttributeType.Options,
                Options = new List<string>()
            }
        };

        _attributeMap = new Dictionary<string, Guid>
        {
            {"Attribute 1", _attributes[0].Id},
            {"Attribute 2", _attributes[1].Id}
        };

        _stateMap = new Dictionary<int, string>
        {
            {1, "State 1"},
            {2, "State 2"}
        };

        _priorityMap = new Dictionary<int, string>
        {
            {1, "Priority 1"},
            {2, "Priority 2"}
        };

        _attributeData = new AttributeData
        {
            AttributeMap = _attributeMap,
            Attributes = _attributes,
            StateMap = _stateMap,
            PriorityMap = _priorityMap
        };

        _testCases = new List<TestCase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Case 1",
                SectionId = _sections[0].Id,
                Attributes = new List<CaseAttribute>(),
                Steps = new List<Step>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Case 2",
                SectionId = _sections[1].Id,
                Attributes = new List<CaseAttribute>(),
                Steps = new List<Step>()
            }
        };

        _testCaseData = new TestCaseData
        {
            TestCases = _testCases,
            Attributes = new List<Attribute>()
        };
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _folderService.ConvertSections()
            .Returns(_sectionData);

        _attributeService.ConvertAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap, _attributeData.AttributeMap, _attributeData.StateMap,
                _attributeData.PriorityMap)
            .Returns(_testCaseData);

        var service = new ExportService(_logger, _client, _folderService, _attributeService, _testCaseService,
            _writeService);

        // Act
        await service.ExportProject();

        // Assert
        await _writeService.Received(1)
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.Received(_testCaseData.TestCases.Count)
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailureGetProject()
    {
        // Arrange
        _client.GetProject()
            .Throws(new Exception("Get project failed"));

        var service = new ExportService(_logger, _client, _folderService, _attributeService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _folderService.DidNotReceive()
            .ConvertSections();

        await _attributeService.DidNotReceive()
            .ConvertAttributes();

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>(),
                Arg.Any<Dictionary<int, string>>(), Arg.Any<Dictionary<int, string>>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailureConvertSections()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _folderService.ConvertSections()
            .Throws(new Exception("Convert sections failed"));

        var service = new ExportService(_logger, _client, _folderService, _attributeService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _attributeService.DidNotReceive()
            .ConvertAttributes();

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>(),
                Arg.Any<Dictionary<int, string>>(), Arg.Any<Dictionary<int, string>>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailureConvertAttributes()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _folderService.ConvertSections()
            .Returns(_sectionData);

        _attributeService.ConvertAttributes()
            .Throws(new Exception("Convert attributes failed"));

        var service = new ExportService(_logger, _client, _folderService, _attributeService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>(),
                Arg.Any<Dictionary<int, string>>(), Arg.Any<Dictionary<int, string>>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailureConvertTestCases()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _folderService.ConvertSections()
            .Returns(_sectionData);

        _attributeService.ConvertAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap, _attributeData.AttributeMap, _attributeData.StateMap,
                _attributeData.PriorityMap)
            .Throws(new Exception("Convert test cases failed"));

        var service = new ExportService(_logger, _client, _folderService, _attributeService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailureWriteMainJson()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _folderService.ConvertSections()
            .Returns(_sectionData);

        _attributeService.ConvertAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap, _attributeData.AttributeMap, _attributeData.StateMap,
                _attributeData.PriorityMap)
            .Returns(_testCaseData);

        _writeService.WriteMainJson(Arg.Any<Root>())
            .Throws(new Exception("Write main json failed"));

        var service = new ExportService(_logger, _client, _folderService, _attributeService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.Received(_testCaseData.TestCases.Count)
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailureWriteTestCase()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _folderService.ConvertSections()
            .Returns(_sectionData);

        _attributeService.ConvertAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap, _attributeData.AttributeMap, _attributeData.StateMap,
                _attributeData.PriorityMap)
            .Returns(_testCaseData);

        _writeService.WriteTestCase(Arg.Any<TestCase>())
            .Throws(new Exception("Write test case failed"));

        var service = new ExportService(_logger, _client, _folderService, _attributeService, _testCaseService,
            _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }
}
