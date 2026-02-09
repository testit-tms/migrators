using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Text;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Models.TestCases.Export;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Implementations;
using ZephyrScaleServerExporter.Services.TestCase;
using ZephyrScaleServerExporterTests.Helpers;
using Attribute = Models.Attribute;
using TestCaseData = ZephyrScaleServerExporter.Models.TestCases.TestCaseData;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class TestCaseBatchServiceTests
{
    private Mock<IOptions<AppConfig>> _mockAppConfig;
    private Mock<ITestCaseCommonService> _mockTestCaseCommonService;
    private Mock<ITestCaseErrorLogService> _mockTestCaseErrorLogService;
    private Mock<ILogger<TestCaseBatchService>> _mockLogger;
    private Mock<IClient> _mockClient;
    private Mock<IWriteService> _mockWriteService;
    private TestCaseBatchService _testCaseBatchService;

    private SectionData _sectionData;
    private Dictionary<string, Attribute> _attributeMap;
    private string _projectId;
    private TestCaseExportRequiredModel _prepDataModel;
    private string _testDirectory;
    private TextReader _originalConsoleIn;
    private string _originalCurrentDirectory;

    [SetUp]
    public void SetUp()
    {
        _mockAppConfig = new Mock<IOptions<AppConfig>>();
        _mockTestCaseCommonService = new Mock<ITestCaseCommonService>();
        _mockTestCaseErrorLogService = new Mock<ITestCaseErrorLogService>();
        _mockLogger = new Mock<ILogger<TestCaseBatchService>>();
        _mockClient = new Mock<IClient>();
        _mockWriteService = new Mock<IWriteService>();

        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _originalCurrentDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        var appConfig = new AppConfig
        {
            ResultPath = _testDirectory,
            Zephyr = new ZephyrConfig
            {
                ProjectKey = "TEST_PROJECT",
                ExportArchived = false,
                Count = 1000
            }
        };
        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);

        _mockWriteService.Setup(w => w.GetBatchNumber()).Returns(1);

        _testCaseBatchService = new TestCaseBatchService(
            _mockAppConfig.Object,
            _mockTestCaseCommonService.Object,
            _mockTestCaseErrorLogService.Object,
            _mockLogger.Object,
            _mockClient.Object,
            _mockWriteService.Object);

        var mainSection = new Section
        {
            Id = Guid.NewGuid(),
            Name = "Main Section",
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        };

        var nestedSection = new Section
        {
            Id = Guid.NewGuid(),
            Name = "Nested Section",
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        };

        mainSection.Sections.Add(nestedSection);

        _sectionData = new SectionData
        {
            MainSection = mainSection,
            SectionMap = new Dictionary<string, Guid>
            {
                { "Main Section", mainSection.Id },
                { "Nested Section", nestedSection.Id }
            },
            AllSections = new Dictionary<string, Section>
            {
                { "Main Section", mainSection },
                { "Nested Section", nestedSection }
            }
        };

        _attributeMap = new Dictionary<string, Attribute>
        {
            {
                "Priority",
                new Attribute
                {
                    Id = Guid.NewGuid(),
                    Name = "Priority",
                    Type = AttributeType.Options,
                    IsRequired = true,
                    IsActive = true,
                    Options = new List<string> { "High", "Medium", "Low" }
                }
            },
            {
                "Status",
                new Attribute
                {
                    Id = Guid.NewGuid(),
                    Name = "Status",
                    Type = AttributeType.Options,
                    IsRequired = false,
                    IsActive = true,
                    Options = new List<string> { "Active", "Inactive" }
                }
            }
        };

        _projectId = TestDataHelper.GenerateProjectId().ToString();

        _prepDataModel = new TestCaseExportRequiredModel
        {
            OwnersAttribute = new Attribute
            {
                Id = Guid.NewGuid(),
                Name = "Owner",
                Type = AttributeType.Options,
                IsRequired = false,
                IsActive = true,
                Options = new List<string> { "User1", "User2" }
            },
            StatusData = new StatusData
            {
                StringStatuses = "\"Active\",\"Inactive\"",
                StatusAttribute = new Attribute
                {
                    Id = Guid.NewGuid(),
                    Name = "Status",
                    Type = AttributeType.Options,
                    IsRequired = false,
                    IsActive = true,
                    Options = new List<string> { "Active", "Inactive" }
                }
            },
            RequiredAttributeNames = new List<string> { "Priority" }
        };

        EnvironmentExitInterceptor.StartIntercepting();
        EnvironmentExitInterceptor.ClearExitCalls();
        _originalConsoleIn = Console.In;
        Console.SetIn(new StringReader("\n"));
    }

    [TearDown]
    public void TearDown()
    {
        EnvironmentExitInterceptor.StopIntercepting();
        Console.SetIn(_originalConsoleIn);

        try
        {
            var batchFile = "TEST_PROJECT-batch.txt";
            if (File.Exists(batchFile))
            {
                File.Delete(batchFile);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        try
        {
            Directory.SetCurrentDirectory(_originalCurrentDirectory);
        }
        catch
        {
            // Ignore cleanup errors
        }

        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region ExportTestCasesBatch

    [Test]
    public async Task ExportTestCasesBatch_WithSingleBatch_ReturnsCorrectTestCaseData()
    {
        // Arrange
        var testCaseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var testCases = TestDataHelper.CreateTestZephyrTestCases(50);
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(testCaseIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), testCases },
            { (100, 100), new List<ZephyrTestCase>() }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(
                testCases,
                _sectionData,
                _attributeMap,
                _prepDataModel.RequiredAttributeNames,
                _prepDataModel.OwnersAttribute))
            .ReturnsAsync(testCaseIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, testCaseIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EquivalentTo(testCaseIds));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        _mockTestCaseCommonService.Verify(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId), Times.Once);
        _mockTestCaseCommonService.VerifyGetTestCasesByConfigBatchesDirect(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService.Verify(s => s.WriteTestCasesAsync(
            testCases,
            _sectionData,
            _attributeMap,
            _prepDataModel.RequiredAttributeNames,
            _prepDataModel.OwnersAttribute), Times.Once);
        _mockTestCaseCommonService.Verify(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, testCaseIds), Times.Once);

        VerifyLogInformation("Converting test cases");
        VerifyLogInformation("test cases and wrote");
    }

    [Test]
    public async Task ExportTestCasesBatch_WithMultipleBatches_ReturnsCorrectTestCaseData()
    {
        // Arrange
        var batch1 = TestDataHelper.CreateTestZephyrTestCases(100);
        var batch2 = TestDataHelper.CreateTestZephyrTestCases(100);
        var batch3 = TestDataHelper.CreateTestZephyrTestCases(50);
        var emptyBatch = new List<ZephyrTestCase>();

        var ids1 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var ids2 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var ids3 = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var allIds = ids1.Concat(ids2).Concat(ids3).ToList();
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(allIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), batch1 },
            { (100, 100), batch2 },
            { (200, 100), batch3 },
            { (300, 100), emptyBatch }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(batch1, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids1);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(batch2, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids2);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(batch3, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids3);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, allIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(250));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        _mockTestCaseCommonService.VerifyGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService.Verify(s => s.WriteTestCasesAsync(
            It.IsAny<List<ZephyrTestCase>>(),
            _sectionData,
            _attributeMap,
            _prepDataModel.RequiredAttributeNames,
            _prepDataModel.OwnersAttribute), Times.Exactly(3));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test cases and wrote")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task ExportTestCasesBatch_WithExportArchived_ProcessesActiveAndArchivedTestCases()
    {
        // Arrange
        SetupAppConfig(true);

        var activeTestCases = TestDataHelper.CreateTestZephyrTestCases(50);
        var archivedTestCases = TestDataHelper.CreateTestZephyrTestCases(30);
        var emptyBatch = new List<ZephyrTestCase>();

        var activeIds = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var archivedIds = Enumerable.Range(0, 30).Select(_ => Guid.NewGuid()).ToList();
        var allIds = activeIds.Concat(archivedIds).ToList();
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(allIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var activeBatches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), activeTestCases },
            { (100, 100), emptyBatch }
        };

        var archivedBatches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), archivedTestCases },
            { (100, 100), emptyBatch }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            activeBatches);

        _mockTestCaseCommonService.SetupGetArchivedTestCasesBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            archivedBatches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(activeTestCases, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(activeIds);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(archivedTestCases, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(archivedIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, allIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(80));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        _mockTestCaseCommonService.VerifyGetTestCasesByConfigBatchesDirect(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            activeBatches);

        _mockTestCaseCommonService.VerifyGetArchivedTestCasesBatchesDirect(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            archivedBatches);
    }

    [Test]
    public async Task ExportTestCasesBatch_WithMultipleBatchesAndArchived_ProcessesAllCorrectly()
    {
        // Arrange
        SetupAppConfig(true);

        var activeBatch1 = TestDataHelper.CreateTestZephyrTestCases(100);
        var activeBatch2 = TestDataHelper.CreateTestZephyrTestCases(50);
        var archivedBatch1 = TestDataHelper.CreateTestZephyrTestCases(100);
        var archivedBatch2 = TestDataHelper.CreateTestZephyrTestCases(30);
        var emptyBatch = new List<ZephyrTestCase>();

        var activeIds1 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var activeIds2 = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var archivedIds1 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var archivedIds2 = Enumerable.Range(0, 30).Select(_ => Guid.NewGuid()).ToList();
        var allIds = activeIds1.Concat(activeIds2).Concat(archivedIds1).Concat(archivedIds2).ToList();
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(allIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var activeBatches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), activeBatch1 },
            { (100, 100), activeBatch2 },
            { (200, 100), emptyBatch }
        };

        var archivedBatches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), archivedBatch1 },
            { (100, 100), archivedBatch2 },
            { (200, 100), emptyBatch }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            activeBatches);

        _mockTestCaseCommonService.SetupGetArchivedTestCasesBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            archivedBatches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(activeBatch1, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(activeIds1);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(activeBatch2, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(activeIds2);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(archivedBatch1, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(archivedIds1);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(archivedBatch2, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(archivedIds2);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, allIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(280));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        _mockTestCaseCommonService.Verify(s => s.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.IsAny<int>(),
            It.Is<int>(x => x == 100),
            It.Is<string>(x => x == _prepDataModel.StatusData.StringStatuses)), Times.Exactly(3));

        _mockTestCaseCommonService.Verify(s => s.GetArchivedTestCases(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.IsAny<int>(),
            It.Is<int>(x => x == 100),
            It.Is<string>(x => x == _prepDataModel.StatusData.StringStatuses)), Times.Exactly(3));

        _mockTestCaseCommonService.Verify(s => s.WriteTestCasesAsync(
            It.IsAny<List<ZephyrTestCase>>(),
            _sectionData,
            _attributeMap,
            _prepDataModel.RequiredAttributeNames,
            _prepDataModel.OwnersAttribute), Times.Exactly(4));
    }

    [Test]
    public async Task ExportTestCasesBatch_WithEmptyResult_ReturnsEmptyTestCaseData()
    {
        // Arrange
        var emptyBatch = new List<ZephyrTestCase>();
        var emptyIds = new List<Guid>();
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(emptyIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        _mockTestCaseCommonService.SetupGetTestCasesByConfig(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            0,
            100,
            emptyBatch);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, emptyIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.Empty);
        });

        _mockTestCaseCommonService.Verify(s => s.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.Is<int>(x => x == 0),
            It.Is<int>(x => x == 100),
            It.Is<string>(x => x == _prepDataModel.StatusData.StringStatuses)), Times.Once);

        _mockTestCaseCommonService.Verify(s => s.WriteTestCasesAsync(
            It.IsAny<List<ZephyrTestCase>>(),
            It.IsAny<SectionData>(),
            It.IsAny<Dictionary<string, Attribute>>(),
            It.IsAny<List<string>>(),
            It.IsAny<Attribute>()), Times.Never);
    }

    [Test]
    public async Task ExportTestCasesBatch_WithBatchFileExists_ReadsExistingNamesAndFiltersCases()
    {
        // Arrange
        var batchFile = "TEST_PROJECT-batch.txt";
        var existingNames = new[] { "Test Case 1", "Test Case 2", "Test Case 3" };
        File.WriteAllLines(batchFile, existingNames);

        var testCases = TestDataHelper.CreateTestZephyrTestCases(5);
        testCases[0].Name = "Test Case 1";
        testCases[1].Name = "Test Case 2";
        testCases[2].Name = "Test Case 3";
        testCases[3].Name = "Test Case 4";
        testCases[4].Name = "Test Case 5";

        var filteredCases = new List<ZephyrTestCase> { testCases[3], testCases[4] };
        var filteredIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(filteredIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), testCases },
            { (100, 100), new List<ZephyrTestCase>() }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(
                It.Is<List<ZephyrTestCase>>(l => l.Count == 2 && l.All(tc => filteredCases.Contains(tc))),
                _sectionData,
                _attributeMap,
                _prepDataModel.RequiredAttributeNames,
                _prepDataModel.OwnersAttribute))
            .ReturnsAsync(filteredIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, filteredIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EquivalentTo(filteredIds));
            Assert.That(File.Exists(batchFile), Is.True);

            var fileContent = File.ReadAllLines(batchFile);
            Assert.That(fileContent, Contains.Item("Test Case 1"));
            Assert.That(fileContent, Contains.Item("Test Case 2"));
            Assert.That(fileContent, Contains.Item("Test Case 3"));
            Assert.That(fileContent, Contains.Item("Test Case 4"));
            Assert.That(fileContent, Contains.Item("Test Case 5"));
        });

        _mockTestCaseCommonService.Verify(s => s.WriteTestCasesAsync(
            It.Is<List<ZephyrTestCase>>(l => l.Count == 2),
            _sectionData,
            _attributeMap,
            _prepDataModel.RequiredAttributeNames,
            _prepDataModel.OwnersAttribute), Times.Once);
    }

    [Test]
    public async Task ExportTestCasesBatch_WithAllCasesFiltered_SkipsBatchAndContinues()
    {
        // Arrange
        var batchFile = "TEST_PROJECT-batch.txt";
        var existingNames = new[] { "Test Case 1", "Test Case 2" };
        File.WriteAllLines(batchFile, existingNames);

        var batch1 = TestDataHelper.CreateTestZephyrTestCases(2);
        var batch2 = TestDataHelper.CreateTestZephyrTestCases(50);

        // Rename batch2 cases to avoid conflicts with batch file names
        for (int i = 0; i < batch2.Count; i++)
        {
            batch2[i].Name = $"Test Case {i + 100}"; // Use names starting from 100 to avoid conflicts
        }

        var ids2 = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(ids2);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), batch1 },
            { (100, 100), batch2 },
            { (200, 100), new List<ZephyrTestCase>() }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(
                It.Is<List<ZephyrTestCase>>(l => l.Count == 50),
                _sectionData,
                _attributeMap,
                _prepDataModel.RequiredAttributeNames,
                _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids2);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, ids2))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EquivalentTo(ids2));
        });

        _mockTestCaseCommonService.Verify(s => s.WriteTestCasesAsync(
            It.Is<List<ZephyrTestCase>>(l => l.Count == 50),
            _sectionData,
            _attributeMap,
            _prepDataModel.RequiredAttributeNames,
            _prepDataModel.OwnersAttribute), Times.Once);
    }

    [Test]
    public async Task ExportTestCasesBatch_WithBatchLimitReached_StopsProcessing()
    {
        // Arrange
        SetupAppConfig(false, 150);

        var batch1 = TestDataHelper.CreateTestZephyrTestCases(100);
        var batch2 = TestDataHelper.CreateTestZephyrTestCases(50);
        var batch3 = TestDataHelper.CreateTestZephyrTestCases(50);

        var ids1 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var ids2 = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var allIds = ids1.Concat(ids2).ToList();
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(allIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), batch1 },
            { (100, 100), batch2 },
            { (200, 100), batch3 },
            { (300, 100), new List<ZephyrTestCase>() }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(batch1, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids1);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(batch2, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids2);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, allIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(150));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        VerifyLogInformation("Batch finished with 150 test cases");
    }

    [Test]
    public async Task ExportTestCasesBatch_WithZeroCasesAndNoPreviousData_CallsEnvironmentExit()
    {
        // Arrange
        var emptyBatch = new List<ZephyrTestCase>();
        var emptyIds = new List<Guid>();

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        _mockTestCaseCommonService.SetupGetTestCasesByConfig(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            0,
            100,
            emptyBatch);

        // Act
        var task = _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);
        await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));

        // Assert
        var exitCalls = EnvironmentExitInterceptor.GetExitCalls();
        Assert.Multiple(() =>
        {
            Assert.That(exitCalls, Has.Count.EqualTo(1));
            Assert.That(exitCalls[0], Is.EqualTo(1));
        });

        VerifyLogInformation("[SUCCESS] This is the last batch, no more data for export exists");
        VerifyLogInformation("total batches: 0");
    }

    [Test]
    public async Task ExportTestCasesBatch_WithZeroCasesAndPreviousData_SavesMainJson()
    {
        // Arrange
        var batch1 = TestDataHelper.CreateTestZephyrTestCases(50);
        var emptyBatch = new List<ZephyrTestCase>();

        var ids1 = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(ids1);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), batch1 },
            { (100, 100), emptyBatch }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(batch1, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids1);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, ids1))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EquivalentTo(ids1));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        VerifyLogInformation("[SUCCESS] This is the last batch, no more data for export exists");
        VerifyLogInformation("Last export: 50 test cases, total batches: 1");
    }

    [Test]
    public async Task ExportTestCasesBatch_WithRetryLogic_RetriesOnExceptionAndContinuesProcessing()
    {
        // Arrange
        var batch1 = TestDataHelper.CreateTestZephyrTestCases(100);
        var batch2 = TestDataHelper.CreateTestZephyrTestCases(50);
        var emptyBatch = new List<ZephyrTestCase>();

        var ids1 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var ids2 = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var allIds = ids1.Concat(ids2).ToList();

        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(allIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        _mockTestCaseCommonService
            .SetupSequence(s => s.GetTestCasesByConfig(
                _mockAppConfig.Object,
                _mockClient.Object,
                It.Is<int>(x => x == 0),
                It.Is<int>(x => x == 100),
                It.Is<string>(x => x == _prepDataModel.StatusData.StringStatuses)))
            .ThrowsAsync(new Exception("First batch error"))
            .ReturnsAsync(batch1);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (100, 100), batch2 },
            { (200, 100), emptyBatch }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(batch1, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids1);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(batch2, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(ids2);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, allIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(150));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        _mockTestCaseCommonService.Verify(s => s.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.Is<int>(x => x == 0),
            It.Is<int>(x => x == 100),
            It.Is<string>(x => x == _prepDataModel.StatusData.StringStatuses)), Times.AtLeast(2));

        VerifyRetryLogging();
    }

    [Test]
    public async Task ExportTestCasesBatch_WithMaxRetriesExceeded_CallsEnvironmentExit()
    {
        // Arrange
        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        _mockTestCaseCommonService
            .Setup(s => s.GetTestCasesByConfig(
                _mockAppConfig.Object,
                _mockClient.Object,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Persistent error"));

        // Act
        var task = _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);
        
        // Wait for Environment.Exit to be called (check every 100ms)
        var timeout = TimeSpan.FromSeconds(20);
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            var exitCallsCheck = EnvironmentExitInterceptor.GetExitCalls();
            if (exitCallsCheck.Count > 0)
            {
                // Environment.Exit was called, break the loop
                break;
            }
            await Task.Delay(100);
        }

        // Assert
        var exitCalls = EnvironmentExitInterceptor.GetExitCalls();
        Assert.Multiple(() =>
        {
            Assert.That(exitCalls, Has.Count.GreaterThanOrEqualTo(1), $"Environment.Exit should be called at least once, but was called {exitCalls.Count} times");
            Assert.That(exitCalls[0], Is.EqualTo(1), "Exit code should be 1");
        });

        _mockTestCaseCommonService.Verify(s => s.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>()), Times.AtLeast(5));

        VerifyLogCritical("Failed to get test cases after 5 attempts in batch mode. Exiting.");
    }

    [Test]
    public async Task ExportTestCasesBatch_WithExceptionInArchivedProcessing_HandlesGracefully()
    {
        // Arrange
        SetupAppConfig(true);

        var activeTestCases = TestDataHelper.CreateTestZephyrTestCases(50);
        var archivedTestCases = TestDataHelper.CreateTestZephyrTestCases(30);
        var emptyBatch = new List<ZephyrTestCase>();

        var activeIds = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var archivedIds = Enumerable.Range(0, 30).Select(_ => Guid.NewGuid()).ToList();
        var allIds = activeIds.Concat(archivedIds).ToList();

        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(allIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var activeBatches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), activeTestCases },
            { (100, 100), emptyBatch }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            activeBatches);

        _mockTestCaseCommonService
            .SetupSequence(s => s.GetArchivedTestCases(
                _mockAppConfig.Object,
                _mockClient.Object,
                It.Is<int>(x => x == 0),
                It.Is<int>(x => x == 100),
                It.Is<string>(x => x == _prepDataModel.StatusData.StringStatuses)))
            .ThrowsAsync(new Exception("Archived error"))
            .ReturnsAsync(archivedTestCases);

        _mockTestCaseCommonService.SetupGetArchivedTestCases(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            100,
            100,
            emptyBatch);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(activeTestCases, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(activeIds);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(archivedTestCases, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(archivedIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, allIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(80));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        VerifyRetryLogging();
    }

    [Test]
    public void ExportTestCasesBatch_WithExceptionInWriteFiltered_LogsErrorAndThrows()
    {
        // Arrange
        var testCases = TestDataHelper.CreateTestZephyrTestCases(50);
        var exception = new Exception("Write error");

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), testCases },
            { (100, 100), new List<ZephyrTestCase>() }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(testCases, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ThrowsAsync(exception);

        // Act & Assert
        Assert.That(
            async () => await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId),
            Throws.Exception.EqualTo(exception));

        _mockTestCaseErrorLogService.Verify(s => s.LogError(
            exception,
            "An error occurred during TestCaseBatchService tc postprocessing",
            null,
            testCases), Times.Once);

        VerifyLogError("An error occurred during TestCaseBatchService tc postprocessing");
    }

    [Test]
    [TestCase(50)]
    [TestCase(75)]
    public async Task ExportTestCasesBatch_WithCustomMaxResults_RespectsConfigLimit(int maxCountPerBatch)
    {
        // Arrange
        SetupAppConfig(false, maxCountPerBatch);

        var testCases = TestDataHelper.CreateTestZephyrTestCases(maxCountPerBatch);
        var testCaseIds = Enumerable.Range(0, maxCountPerBatch).Select(_ => Guid.NewGuid()).ToList();
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(testCaseIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, maxCountPerBatch), testCases },
            { (maxCountPerBatch, maxCountPerBatch), new List<ZephyrTestCase>() }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(testCases, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(testCaseIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, testCaseIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(maxCountPerBatch));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        _mockTestCaseCommonService.Verify(s => s.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.Is<int>(x => x == 0),
            It.Is<int>(x => x == maxCountPerBatch),
            It.Is<string>(x => x == _prepDataModel.StatusData.StringStatuses)), Times.Once);
    }

    [Test]
    public async Task ExportTestCasesBatch_WithBatchFileNotExists_CreatesNewFileAndAddsProcessedCases()
    {
        // Arrange
        var batchFile = "TEST_PROJECT-batch.txt";
        Assert.That(File.Exists(batchFile), Is.False);

        var testCases = TestDataHelper.CreateTestZephyrTestCases(3);
        testCases[0].Name = "Case 1";
        testCases[1].Name = "Case 2";
        testCases[2].Name = "Case 3";

        var testCaseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(testCaseIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId))
            .ReturnsAsync(_prepDataModel);

        var batches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), testCases },
            { (100, 100), new List<ZephyrTestCase>() }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(testCases, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(testCaseIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, testCaseIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseBatchService.ExportTestCasesBatch(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EquivalentTo(testCaseIds));
            Assert.That(File.Exists(batchFile), Is.True);
            var fileContent = File.ReadAllLines(batchFile);
            Assert.That(fileContent, Has.Length.EqualTo(3));
            Assert.That(fileContent, Contains.Item("Case 1"));
            Assert.That(fileContent, Contains.Item("Case 2"));
            Assert.That(fileContent, Contains.Item("Case 3"));
        });
    }

    #endregion

    #region Helper Methods

    private void SetupAppConfig(bool exportArchived, int count = 1000)
    {
        var appConfig = new AppConfig
        {
            ResultPath = _testDirectory,
            Zephyr = new ZephyrConfig
            {
                ProjectKey = "TEST_PROJECT",
                ExportArchived = exportArchived,
                Count = count
            }
        };
        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);
        _testCaseBatchService = new TestCaseBatchService(
            _mockAppConfig.Object,
            _mockTestCaseCommonService.Object,
            _mockTestCaseErrorLogService.Object,
            _mockLogger.Object,
            _mockClient.Object,
            _mockWriteService.Object);
    }

    private void VerifyLogInformation(string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyLogError(string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyLogWarning(string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyLogCritical(string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyRetryLogging()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get test cases starting from") && v.ToString()!.Contains("Attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Waiting 3 seconds before next attempt in batch mode")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
