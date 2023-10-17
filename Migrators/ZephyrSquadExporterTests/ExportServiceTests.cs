using JsonWriter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrSquadExporter.Models;
using ZephyrSquadExporter.Services;

namespace ZephyrSquadExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger;
    private IFolderService _folderService;
    private ITestCaseService _testCaseService;
    private IWriteService _writeService;
    private IConfiguration _configuration;

    private SectionData _sectionData;
    private List<TestCase> _testCases;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExportService>>();
        _folderService = Substitute.For<IFolderService>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _writeService = Substitute.For<IWriteService>();

        var inMemorySettings = new Dictionary<string, string> {
            {"zephyr:projectName", "ProjectName"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _sectionData = new SectionData
        {
            Sections = new List<Section>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Section 1",
                    PostconditionSteps = new List<Step>(),
                    PreconditionSteps = new List<Step>(),
                    Sections = new List<Section>()
                }
            },
            SectionMap = new Dictionary<string, ZephyrSection>
            {
                {
                    "Section 1", new ZephyrSection
                    {
                        Id = Guid.NewGuid(),
                        IsFolder = false,
                        CycleId = "1"
                    }
                }
            }
        };

        _testCases = new List<TestCase>
        {
            new()
            {
                Name = "Test Case 1",
                Id = Guid.NewGuid(),
            }
        };
    }

    [Test]
    public async Task ExportProject_FailedGetSections()
    {
        // Arrange
        _folderService.GetSections()
            .Throws(new Exception("Failed to get sections"));

        var service = new ExportService(_logger, _folderService, _testCaseService, _writeService, _configuration);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Dictionary<string, ZephyrSection>>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertTestCases()
    {
        // Arrange
        _folderService.GetSections()
            .Returns(_sectionData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap)
            .Throws(new Exception("Failed to convert test cases"));

        var service = new ExportService(_logger, _folderService, _testCaseService, _writeService, _configuration);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteTestCase()
    {
        // Arrange
        _folderService.GetSections()
            .Returns(_sectionData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap)
            .Returns(_testCases);

        _writeService.WriteTestCase(_testCases[0])
            .Throws(new Exception("Failed to write test case"));

        var service = new ExportService(_logger, _folderService, _testCaseService, _writeService, _configuration);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteMainJson()
    {
        // Arrange
        _folderService.GetSections()
            .Returns(new SectionData
            {
                Sections = new List<Section>(),
                SectionMap = new Dictionary<string, ZephyrSection>()
            });

        _testCaseService.ConvertTestCases(Arg.Any<Dictionary<string, ZephyrSection>>())
            .Returns(_testCases);

        _writeService.WriteMainJson(Arg.Any<Root>())
            .Throws(new Exception("Failed to write main json"));

        var service = new ExportService(_logger, _folderService, _testCaseService, _writeService, _configuration);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.ExportProject());
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        _folderService.GetSections()
            .Returns(_sectionData);

        _testCaseService.ConvertTestCases(_sectionData.SectionMap)
            .Returns(_testCases);

        var service = new ExportService(_logger, _folderService, _testCaseService, _writeService, _configuration);

        // Act
        await service.ExportProject();

        // Assert
        await _writeService.Received()
            .WriteMainJson(Arg.Any<Root>());
    }
}
