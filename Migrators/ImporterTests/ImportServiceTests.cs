using Importer.Client;
using Importer.Models;
using Importer.Services;
using Importer.Services.Implementations;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Attribute = Models.Attribute;

namespace ImporterTests;

public class ImportServiceTests
{
    private ILogger<ImportService> _logger = null!;
    private IParserService _parserService = null!;
    private IClientAdapter _clientAdapter = null!;
    private IAttributeService _attributeService = null!;
    private ISectionService _sectionService = null!;
    private ISharedStepService _sharedStepService = null!;
    private ITestCaseService _testCaseService = null!;
    private IProjectService _projectService = null!;
    private ImportService _importService = null!;

    private static readonly Guid ProjectId = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private static readonly string ProjectName = "Test Project";
    private Root _mainJsonResult = null!;
    private Dictionary<Guid, Guid> _sections = null!;
    private Dictionary<Guid, TmsAttribute> _attributesMap = null!;
    private Dictionary<Guid, Guid> _sharedSteps = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ImportService>>();
        _parserService = Substitute.For<IParserService>();
        _clientAdapter = Substitute.For<IClientAdapter>();
        _attributeService = Substitute.For<IAttributeService>();
        _sectionService = Substitute.For<ISectionService>();
        _sharedStepService = Substitute.For<ISharedStepService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _projectService = Substitute.For<IProjectService>();

        InitializeTestData();

        _importService = new ImportService(
            _logger,
            _parserService,
            _clientAdapter,
            _attributeService,
            _sectionService,
            _sharedStepService,
            _testCaseService,
            _projectService
        );
    }

    private void InitializeTestData()
    {
        var sectionId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var sharedStepId = Guid.NewGuid();
        var testCaseId = Guid.NewGuid();

        _mainJsonResult = new Root
        {
            ProjectName = ProjectName,
            Sections = new List<Section> { new() { Id = sectionId } },
            Attributes = new List<Attribute> { new() { Id = attributeId } },
            SharedSteps = new List<Guid> { sharedStepId },
            TestCases = new List<Guid> { testCaseId }
        };

        _sections = new Dictionary<Guid, Guid>
        {
            { sectionId, Guid.NewGuid() }
        };

        _attributesMap = new Dictionary<Guid, TmsAttribute>
        {
            { attributeId, new TmsAttribute { Id = Guid.NewGuid() } }
        };

        _sharedSteps = new Dictionary<Guid, Guid>
        {
            { sharedStepId, Guid.NewGuid() }
        };
    }

    [Test]
    public async Task ImportProject_WhenGetMainFileFails_ThrowsException()
    {
        // Arrange
        _parserService.GetMainFile()
            .ThrowsAsync(new Exception("Failed to get main file"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(async () => await _importService.ImportProject());

        Assert.Multiple(async () =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to get main file"));
            await _projectService.DidNotReceive().ImportProject(Arg.Any<string>());
            await _sectionService.DidNotReceive().ImportSections(Arg.Any<Guid>(), Arg.Any<IEnumerable<Section>>());
            await _attributeService.DidNotReceive().ImportAttributes(Arg.Any<Guid>(), Arg.Any<IEnumerable<Attribute>>());
            await _sharedStepService.DidNotReceive().ImportSharedSteps(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Dictionary<Guid, Guid>>(), Arg.Any<Dictionary<Guid, TmsAttribute>>());
            await _testCaseService.DidNotReceive().ImportTestCases(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Dictionary<Guid, Guid>>(), Arg.Any<Dictionary<Guid, TmsAttribute>>(),
                Arg.Any<Dictionary<Guid, Guid>>());
        });
    }

    [Test]
    public async Task ImportProject_WhenImportProjectFails_ThrowsException()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _projectService.ImportProject(ProjectName)
            .ThrowsAsync(new Exception("Failed to import project"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(async () => await _importService.ImportProject());

        Assert.Multiple(async () =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to import project"));
            await _sectionService.DidNotReceive().ImportSections(Arg.Any<Guid>(), Arg.Any<IEnumerable<Section>>());
            await _attributeService.DidNotReceive().ImportAttributes(Arg.Any<Guid>(), Arg.Any<IEnumerable<Attribute>>());
            await _sharedStepService.DidNotReceive().ImportSharedSteps(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Dictionary<Guid, Guid>>(), Arg.Any<Dictionary<Guid, TmsAttribute>>());
            await _testCaseService.DidNotReceive().ImportTestCases(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Dictionary<Guid, Guid>>(), Arg.Any<Dictionary<Guid, TmsAttribute>>(),
                Arg.Any<Dictionary<Guid, Guid>>());
        });
    }

    [Test]
    public async Task ImportProject_WhenImportSectionsFails_ThrowsException()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _projectService.ImportProject(ProjectName).Returns(ProjectId);
        _sectionService.ImportSections(ProjectId, _mainJsonResult.Sections)
            .ThrowsAsync(new Exception("Failed to import sections"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(async () => await _importService.ImportProject());

        Assert.Multiple(async () =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to import sections"));
            await _attributeService.DidNotReceive().ImportAttributes(Arg.Any<Guid>(), Arg.Any<IEnumerable<Attribute>>());
            await _sharedStepService.DidNotReceive().ImportSharedSteps(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Dictionary<Guid, Guid>>(), Arg.Any<Dictionary<Guid, TmsAttribute>>());
            await _testCaseService.DidNotReceive().ImportTestCases(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Dictionary<Guid, Guid>>(), Arg.Any<Dictionary<Guid, TmsAttribute>>(),
                Arg.Any<Dictionary<Guid, Guid>>());
        });
    }

    [Test]
    public async Task ImportProject_WhenSuccessful_ImportsAllComponents()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _projectService.ImportProject(ProjectName).Returns(ProjectId);
        _sectionService.ImportSections(ProjectId, _mainJsonResult.Sections).Returns(_sections);
        _attributeService.ImportAttributes(ProjectId, _mainJsonResult.Attributes).Returns(_attributesMap);
        _sharedStepService.ImportSharedSteps(ProjectId, _mainJsonResult.SharedSteps, _sections, _attributesMap)
            .Returns(_sharedSteps);

        // Act
        await _importService.ImportProject();

        // Assert
        Assert.Multiple(async () =>
        {
            await _parserService.Received(1).GetMainFile();
            await _projectService.Received(1).ImportProject(ProjectName);
            await _sectionService.Received(1).ImportSections(ProjectId, _mainJsonResult.Sections);
            await _attributeService.Received(1).ImportAttributes(ProjectId, _mainJsonResult.Attributes);
            await _sharedStepService.Received(1).ImportSharedSteps(ProjectId, _mainJsonResult.SharedSteps,
                _sections, _attributesMap);
            await _testCaseService.Received(1).ImportTestCases(ProjectId, _mainJsonResult.TestCases,
                _sections, _attributesMap, _sharedSteps);
        });
    }
} 