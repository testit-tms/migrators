using Importer.Client;
using Importer.Models;
using Importer.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Attribute = Models.Attribute;

namespace ImporterTests;

public class ImportServiceTests
{
    private ILogger<ImportService> _logger;
    private IParserService _parserService;
    private IClient _client;
    private IAttributeService _attributeService;
    private ISectionService _sectionService;
    private ISharedStepService _sharedStepService;
    private ITestCaseService _testCaseService;
    private Root _mainJsonResult;
    private List<Section> _sections;
    private List<Attribute> _attributes;
    private Dictionary<Guid, Guid> _sectionsMap;
    private Dictionary<Guid, TmsAttribute> _attributesMap;
    private List<Guid> _sharedSteps;
    private List<Guid> _testCases;
    private Dictionary<Guid, Guid> _sharedStepsMap;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ImportService>>();
        _parserService = Substitute.For<IParserService>();
        _client = Substitute.For<IClient>();
        _attributeService = Substitute.For<IAttributeService>();
        _sectionService = Substitute.For<ISectionService>();
        _sharedStepService = Substitute.For<ISharedStepService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee6"),
                Name = "TestAttribute",
                IsActive = true,
                IsRequired = false,
                Type = AttributeType.String,
                Options = new List<string>()
            },
            new()
            {
                Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad0d"),
                Name = "TestAttribute2",
                IsActive = true,
                IsRequired = false,
                Type = AttributeType.Options,
                Options = new List<string>() { "Option1", "Option2" }
            }
        };
        _sections = new List<Section>
        {
            new()
            {
                Id = Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c88"),
                Name = "TestSection",
                PreconditionSteps = new List<Step>
                {
                    new()
                    {
                        Action = "TestAction",
                        Expected = "TestExpected"
                    }
                },
                PostconditionSteps = new List<Step>
                {
                    new()
                    {
                        Action = "TestAction",
                        Expected = "TestExpected"
                    }
                },
                Sections = new List<Section>
                {
                    new()
                    {
                        Id = Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781de9"),
                        Name = "TestSection02",
                        PreconditionSteps = new List<Step>
                        {
                            new()
                            {
                                Action = "TestAction",
                                Expected = "TestExpected"
                            }
                        },
                        PostconditionSteps = new List<Step>
                        {
                            new()
                            {
                                Action = "TestAction",
                                Expected = "TestExpected"
                            }
                        },
                        Sections = new List<Section>()
                    }
                }
            }
        };
        _sharedSteps = new List<Guid>
        {
            Guid.Parse("cacaec23-cf89-46f8-918e-bfae7003895e"),
            Guid.Parse("ad1b46bc-13c6-400f-af4d-8243c0aec4d2")
        };
        _testCases = new List<Guid>
        {
            Guid.Parse("e6053aae-d79a-4ae5-9dee-9d2e59c2abc9"),
            Guid.Parse("2742c22e-9cc4-428a-ac23-bc344b54a8ea")
        };
        _mainJsonResult = new Root
        {
            ProjectName = "TestProject",
            Attributes = _attributes,
            Sections = _sections,
            SharedSteps = _sharedSteps,
            TestCases = _testCases
        };
        _sectionsMap = new Dictionary<Guid, Guid>
        {
            { Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c88"), Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c10") },
            { Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781de9"), Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781d10") }
        };
        _sharedStepsMap = new Dictionary<Guid, Guid>
        {
            { Guid.Parse("cacaec23-cf89-46f8-918e-bfae7003895e"), Guid.Parse("cacaec23-cf89-46f8-918e-bfae70038910") },
            { Guid.Parse("ad1b46bc-13c6-400f-af4d-8243c0aec4d2"), Guid.Parse("ad1b46bc-13c6-400f-af4d-8243c0aec410") }
        };
        _attributesMap = new Dictionary<Guid, TmsAttribute>
        {
            {
                Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee6"),
                new TmsAttribute
                {
                    Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbe10"),
                    Name = "TestAttribute",
                    IsRequired = false,
                    IsEnabled = true,
                    Type = "String",
                    Options = new List<TmsAttributeOptions>()
                }
            },
            {
                Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad0d"),
                new TmsAttribute
                {
                    Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad10"),
                    Name = "TestAttribute2",
                    IsRequired = false,
                    IsEnabled = true,
                    Type = "Options",
                    Options = new List<TmsAttributeOptions>
                    {
                        new()
                        {
                            Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad11"),
                            Value = "Option1",
                            IsDefault = true
                        },
                        new()
                        {
                            Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad12"),
                            Value = "Option2",
                            IsDefault = false
                        }
                    }
                }
            }
        };
    }

    [Test]
    public async Task ImportProject_FailedParse()
    {
        // Arrange
        _parserService.GetMainFile().ThrowsAsync(new Exception("Failed to parse"));

        var importService = new ImportService(_logger, _parserService, _client, _attributeService, _sectionService,
            _sharedStepService, _testCaseService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await importService.ImportProject());

        // Assert
        await _parserService.Received().GetMainFile();
        await _client.DidNotReceive().CreateProject(Arg.Any<string>());
        await _sectionService.DidNotReceive().ImportSections(Arg.Any<List<Section>>());
        await _attributeService.DidNotReceive().ImportAttributes(Arg.Any<List<Attribute>>());
        await _sharedStepService.DidNotReceive().ImportSharedSteps(Arg.Any<List<Guid>>(),
            Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>());
        await _testCaseService.DidNotReceive().ImportTestCases(Arg.Any<List<Guid>>(), Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>(), Arg.Any<Dictionary<Guid, Guid>>());
    }

    [Test]
    public async Task ImportProject_FailedCreateProject()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _client.CreateProject("TestProject").ThrowsAsync(new Exception("Failed to create"));

        var importService = new ImportService(_logger, _parserService, _client, _attributeService, _sectionService,
            _sharedStepService, _testCaseService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await importService.ImportProject());

        // Assert
        await _parserService.Received().GetMainFile();
        await _client.Received().CreateProject("TestProject");
        await _sectionService.DidNotReceive().ImportSections(Arg.Any<List<Section>>());
        await _attributeService.DidNotReceive().ImportAttributes(Arg.Any<List<Attribute>>());
        await _sharedStepService.DidNotReceive().ImportSharedSteps(Arg.Any<List<Guid>>(),
            Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>());
        await _testCaseService.DidNotReceive().ImportTestCases(Arg.Any<List<Guid>>(), Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>(), Arg.Any<Dictionary<Guid, Guid>>());
    }

    [Test]
    public async Task ImportProject_FailedCreateSections()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _sectionService.ImportSections(_sections).ThrowsAsync(new Exception("Failed to create"));

        var importService = new ImportService(_logger, _parserService, _client, _attributeService, _sectionService,
            _sharedStepService, _testCaseService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await importService.ImportProject());

        // Assert
        await _parserService.Received().GetMainFile();
        await _client.Received().CreateProject("TestProject");
        await _sectionService.Received().ImportSections(_sections);
        await _attributeService.DidNotReceive().ImportAttributes(Arg.Any<List<Attribute>>());
        await _sharedStepService.DidNotReceive().ImportSharedSteps(Arg.Any<List<Guid>>(),
            Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>());
        await _testCaseService.DidNotReceive().ImportTestCases(Arg.Any<List<Guid>>(), Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>(), Arg.Any<Dictionary<Guid, Guid>>());
    }

    [Test]
    public async Task ImportProject_FailedCreateAttributes()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _sectionService.ImportSections(_sections).Returns(_sectionsMap);
        _attributeService.ImportAttributes(_attributes).ThrowsAsync(new Exception("Failed to create"));

        var importService = new ImportService(_logger, _parserService, _client, _attributeService, _sectionService,
            _sharedStepService, _testCaseService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await importService.ImportProject());

        // Assert
        await _parserService.Received().GetMainFile();
        await _client.Received().CreateProject("TestProject");
        await _sectionService.Received().ImportSections(_sections);
        await _attributeService.Received().ImportAttributes(_attributes);
        await _sharedStepService.DidNotReceive().ImportSharedSteps(Arg.Any<List<Guid>>(),
            Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>());
        await _testCaseService.DidNotReceive().ImportTestCases(Arg.Any<List<Guid>>(), Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>(), Arg.Any<Dictionary<Guid, Guid>>());
    }

    [Test]
    public async Task ImportProject_FailedCreateSharedSteps()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _sectionService.ImportSections(_sections).Returns(_sectionsMap);
        _attributeService.ImportAttributes(_attributes).Returns(_attributesMap);
        _sharedStepService.ImportSharedSteps(_sharedSteps, _sectionsMap, _attributesMap)
            .ThrowsAsync(new Exception("Failed to create"));

        var importService = new ImportService(_logger, _parserService, _client, _attributeService, _sectionService,
            _sharedStepService, _testCaseService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await importService.ImportProject());

        // Assert
        await _parserService.Received().GetMainFile();
        await _client.Received().CreateProject("TestProject");
        await _sectionService.Received().ImportSections(_sections);
        await _attributeService.Received().ImportAttributes(_attributes);
        await _sharedStepService.Received().ImportSharedSteps(_sharedSteps, _sectionsMap, _attributesMap);
        await _testCaseService.DidNotReceive().ImportTestCases(Arg.Any<List<Guid>>(), Arg.Any<Dictionary<Guid, Guid>>(),
            Arg.Any<Dictionary<Guid, TmsAttribute>>(), Arg.Any<Dictionary<Guid, Guid>>());
    }

    [Test]
    public async Task ImportProject_FailedCreateTestCases()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _sectionService.ImportSections(_sections).Returns(_sectionsMap);
        _attributeService.ImportAttributes(_attributes).Returns(_attributesMap);
        _sharedStepService.ImportSharedSteps(_sharedSteps, _sectionsMap, _attributesMap)
            .Returns(_sharedStepsMap);
        _testCaseService.ImportTestCases(_testCases, _sectionsMap, _attributesMap, _sharedStepsMap)
            .ThrowsAsync(new Exception("Failed to create"));

        var importService = new ImportService(_logger, _parserService, _client, _attributeService, _sectionService,
            _sharedStepService, _testCaseService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await importService.ImportProject());

        // Assert
        await _parserService.Received().GetMainFile();
        await _client.Received().CreateProject("TestProject");
        await _sectionService.Received().ImportSections(_sections);
        await _attributeService.Received().ImportAttributes(_attributes);
        await _sharedStepService.Received().ImportSharedSteps(_sharedSteps, _sectionsMap, _attributesMap);
        await _testCaseService.Received().ImportTestCases(_testCases, _sectionsMap, _attributesMap, _sharedStepsMap);
    }

    [Test]
    public async Task ImportProject_Success()
    {
        // Arrange
        _parserService.GetMainFile().Returns(_mainJsonResult);
        _sectionService.ImportSections(_sections).Returns(_sectionsMap);
        _attributeService.ImportAttributes(_attributes).Returns(_attributesMap);
        _sharedStepService.ImportSharedSteps(_sharedSteps, _sectionsMap, _attributesMap)
            .Returns(_sharedStepsMap);

        var importService = new ImportService(_logger, _parserService, _client, _attributeService, _sectionService,
            _sharedStepService, _testCaseService);

        // Act
        await importService.ImportProject();

        // Assert
        await _parserService.Received().GetMainFile();
        await _client.Received().CreateProject("TestProject");
        await _sectionService.Received().ImportSections(_sections);
        await _attributeService.Received().ImportAttributes(_attributes);
        await _sharedStepService.Received().ImportSharedSteps(_sharedSteps, _sectionsMap, _attributesMap);
        await _testCaseService.Received().ImportTestCases(_testCases, _sectionsMap, _attributesMap, _sharedStepsMap);
    }
}
