using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TestCollabExporter.Client;
using TestCollabExporter.Models;
using TestCollabExporter.Services;
using Attribute = Models.Attribute;

namespace TestCollabExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger;
    private IClient _client;
    private ISectionService _sectionService;
    private ITestCaseService _testCaseService;
    private ISharedStepService _sharedStepService;
    private IAttributeService _attributeService;
    private IWriteService _writeService;

    private TestCollabCompanies _companies;
    private TestCollabProject _project;
    private AttributeData _attributes;
    private SectionData _sections;
    private SharedStepData _sharedStep;
    private List<TestCase> _testCases;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExportService>>();
        _client = Substitute.For<IClient>();
        _sectionService = Substitute.For<ISectionService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _sharedStepService = Substitute.For<ISharedStepService>();
        _attributeService = Substitute.For<IAttributeService>();
        _writeService = Substitute.For<IWriteService>();

        _companies = new TestCollabCompanies
        {
            Companies = new List<TestCollabCompany>
            {
                new()
                {
                    Id = 1,
                },
                new()
                {
                    Id = 2
                }
            }
        };

        _project = new TestCollabProject
        {
            CompanyId = 1,
            Id = 10,
            Name = "Test Project"
        };

        var attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Attribute 1",
                IsRequired = false,
                IsActive = true,
                Type = AttributeType.String,
                Options = new List<string>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Attribute 2",
                IsRequired = false,
                IsActive = true,
                Type = AttributeType.String,
                Options = new List<string>()
            }
        };

        _attributes = new AttributeData
        {
            Attributes = attributes,
            AttributesMap = attributes.ToDictionary(a => a.Name, a => a.Id)
        };

        var sections = new List<Section>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Section 1",
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = new List<Section>()
            }
        };

        _sections = new SectionData
        {
            Sections = sections,
            SectionMap = new Dictionary<int, Guid>
            {
                { 20, sections[0].Id }
            },
            SharedStepSection = new Section
            {
                Id = Guid.NewGuid(),
                Name = "Shared Steps",
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = new List<Section>()
            }
        };

        var sharedSteps = new List<SharedStep>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Shared Step 1"
            }
        };

        _sharedStep = new SharedStepData
        {
            SharedSteps = sharedSteps,
            SharedStepsMap = new Dictionary<int, Guid>
            {
                { 30, sharedSteps[0].Id }
            }
        };

        _testCases = new List<TestCase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Case 1"
            }
        };
    }

    [Test]
    public async Task ExportProject_FailedGetCompany()
    {
        // Arrange
        _client.GetCompany()
            .Throws(new Exception("Failed to get companies"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _client.DidNotReceive()
            .GetProject(Arg.Any<TestCollabCompanies>());

        await _attributeService.DidNotReceive()
            .ConvertAttributes(Arg.Any<int>());

        await _sectionService.DidNotReceive()
            .ConvertSections(Arg.Any<int>());

        await _sharedStepService.DidNotReceive()
            .ConvertSharedSteps(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<List<Guid>>());

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<int>(), Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>(),
                Arg.Any<Dictionary<int, Guid>>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailedGetProject()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Throws(new Exception("Failed to get project"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _attributeService.DidNotReceive()
            .ConvertAttributes(Arg.Any<int>());

        await _sectionService.DidNotReceive()
            .ConvertSections(Arg.Any<int>());

        await _sharedStepService.DidNotReceive()
            .ConvertSharedSteps(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<List<Guid>>());

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<int>(), Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>(),
                Arg.Any<Dictionary<int, Guid>>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailedConvertAttributes()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Returns(_project);

        _attributeService.ConvertAttributes(_project.CompanyId)
            .Throws(new Exception("Failed to convert attributes"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _sectionService.DidNotReceive()
            .ConvertSections(Arg.Any<int>());

        await _sharedStepService.DidNotReceive()
            .ConvertSharedSteps(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<List<Guid>>());

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<int>(), Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>(),
                Arg.Any<Dictionary<int, Guid>>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailedConvertSections()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Returns(_project);

        _attributeService.ConvertAttributes(_project.CompanyId)
            .Returns(_attributes);

        _sectionService.ConvertSections(_project.Id)
            .Throws(new Exception("Failed to convert sections"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _sharedStepService.DidNotReceive()
            .ConvertSharedSteps(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<List<Guid>>());

        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<int>(), Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>(),
                Arg.Any<Dictionary<int, Guid>>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailedConvertSharedSteps()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Returns(_project);

        _attributeService.ConvertAttributes(_project.CompanyId)
            .Returns(_attributes);

        _sectionService.ConvertSections(_project.Id)
            .Returns(_sections);

        _sharedStepService.ConvertSharedSteps(_project.Id, _sections.SharedStepSection.Id,
                Arg.Is<List<Guid>>(s => s.SequenceEqual(_attributes.AttributesMap.Values.ToList())))
            .Throws(new Exception("Failed to convert shared steps"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<int>(), Arg.Any<Dictionary<int, Guid>>(), Arg.Any<Dictionary<string, Guid>>(),
                Arg.Any<Dictionary<int, Guid>>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailedConvertTestCases()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Returns(_project);

        _attributeService.ConvertAttributes(_project.CompanyId)
            .Returns(_attributes);

        _sectionService.ConvertSections(_project.Id)
            .Returns(_sections);

        _sharedStepService.ConvertSharedSteps(_project.Id, _sections.SharedStepSection.Id,
                Arg.Is<List<Guid>>(s => s.SequenceEqual(_attributes.AttributesMap.Values.ToList())))
            .Returns(_sharedStep);

        _testCaseService.ConvertTestCases(_project.Id,
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(d => d.SequenceEqual(_attributes.AttributesMap)),
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sharedStep.SharedStepsMap)))
            .Throws(new Exception("Failed to convert test cases"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }


    [Test]
    public async Task ExportProject_FailedWriteSharedStep()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Returns(_project);

        _attributeService.ConvertAttributes(_project.CompanyId)
            .Returns(_attributes);

        _sectionService.ConvertSections(_project.Id)
            .Returns(_sections);

        _sharedStepService.ConvertSharedSteps(_project.Id, _sections.SharedStepSection.Id,
                Arg.Is<List<Guid>>(s => s.SequenceEqual(_attributes.AttributesMap.Values.ToList())))
            .Returns(_sharedStep);

        _testCaseService.ConvertTestCases(_project.Id,
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(d => d.SequenceEqual(_attributes.AttributesMap)),
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sharedStep.SharedStepsMap)))
            .Returns(_testCases);

        _writeService.WriteSharedStep(_sharedStep.SharedSteps[0])
            .Throws(new Exception("Failed to write shared step"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());
    }

    [Test]
    public async Task ExportProject_FailedWriteTestCase()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Returns(_project);

        _attributeService.ConvertAttributes(_project.CompanyId)
            .Returns(_attributes);

        _sectionService.ConvertSections(_project.Id)
            .Returns(_sections);

        _sharedStepService.ConvertSharedSteps(_project.Id, _sections.SharedStepSection.Id,
                Arg.Is<List<Guid>>(s => s.SequenceEqual(_attributes.AttributesMap.Values.ToList())))
            .Returns(_sharedStep);

        _testCaseService.ConvertTestCases(_project.Id,
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(d => d.SequenceEqual(_attributes.AttributesMap)),
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sharedStep.SharedStepsMap)))
            .Returns(_testCases);

        _writeService.WriteTestCase(_testCases[0])
            .Throws(new Exception("Failed to write test case"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.Received()
            .WriteSharedStep(Arg.Any<SharedStep>());
    }

    [Test]
    public async Task ExportProject_FailedWriteMainJson()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Returns(_project);

        _attributeService.ConvertAttributes(_project.CompanyId)
            .Returns(_attributes);

        _sectionService.ConvertSections(_project.Id)
            .Returns(_sections);

        _sharedStepService.ConvertSharedSteps(_project.Id, _sections.SharedStepSection.Id,
                Arg.Is<List<Guid>>(s => s.SequenceEqual(_attributes.AttributesMap.Values.ToList())))
            .Returns(_sharedStep);

        _testCaseService.ConvertTestCases(_project.Id,
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(d => d.SequenceEqual(_attributes.AttributesMap)),
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sharedStep.SharedStepsMap)))
            .Returns(_testCases);

        _writeService.WriteMainJson(Arg.Any<Root>() )
            .Throws(new Exception("Failed to write main json"));

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.Received()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.Received()
            .WriteSharedStep(Arg.Any<SharedStep>());
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        _client.GetCompany()
            .Returns(_companies);

        _client.GetProject(_companies)
            .Returns(_project);

        _attributeService.ConvertAttributes(_project.CompanyId)
            .Returns(_attributes);

        _sectionService.ConvertSections(_project.Id)
            .Returns(_sections);

        _sharedStepService.ConvertSharedSteps(_project.Id, _sections.SharedStepSection.Id,
                Arg.Is<List<Guid>>(s => s.SequenceEqual(_attributes.AttributesMap.Values.ToList())))
            .Returns(_sharedStep);

        _testCaseService.ConvertTestCases(_project.Id,
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sections.SectionMap)),
                Arg.Is<Dictionary<string, Guid>>(d => d.SequenceEqual(_attributes.AttributesMap)),
                Arg.Is<Dictionary<int, Guid>>(d => d.SequenceEqual(_sharedStep.SharedStepsMap)))
            .Returns(_testCases);

        var exportService = new ExportService(_logger, _client, _sectionService, _testCaseService, _sharedStepService,
            _attributeService, _writeService);

        // Act
        await exportService.ExportProject();

        // Assert
        await _writeService.Received(1)
            .WriteMainJson(Arg.Any<Root>());

        await _writeService.Received()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.Received()
            .WriteTestCase(Arg.Any<TestCase>());
    }
}
