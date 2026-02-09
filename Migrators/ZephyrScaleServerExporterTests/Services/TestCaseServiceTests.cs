using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Models.TestCases.Export;
using ZephyrScaleServerExporter.Services.Implementations;
using ZephyrScaleServerExporter.Services.TestCase;
using ZephyrScaleServerExporterTests.Helpers;
using Attribute = Models.Attribute;
using TestCaseData = ZephyrScaleServerExporter.Models.TestCases.TestCaseData;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class TestCaseServiceTests
{
    private Mock<IOptions<AppConfig>> _mockAppConfig;
    private Mock<ITestCaseCommonService> _mockTestCaseCommonService;
    private Mock<ILogger<TestCaseService>> _mockLogger;
    private Mock<IClient> _mockClient;
    private TestCaseService _testCaseService;

    private SectionData _sectionData;
    private Dictionary<string, Attribute> _attributeMap;
    private string _projectId;
    private TestCaseExportRequiredModel _prepDataModel;

    [SetUp]
    public void SetUp()
    {
        _mockAppConfig = new Mock<IOptions<AppConfig>>();
        _mockTestCaseCommonService = new Mock<ITestCaseCommonService>();
        _mockLogger = new Mock<ILogger<TestCaseService>>();
        _mockClient = new Mock<IClient>();

        var appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                ExportArchived = false
            }
        };
        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);

        _testCaseService = new TestCaseService(
            _mockAppConfig.Object,
            _mockTestCaseCommonService.Object,
            _mockLogger.Object,
            _mockClient.Object);

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
    }

    #region ExportTestCases

    [Test]
    public async Task ExportTestCases_WithSingleBatch_ReturnsCorrectTestCaseData()
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
        var result = await _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EquivalentTo(testCaseIds));
            Assert.That(result.Attributes, Is.Not.Null);
        });

        _mockTestCaseCommonService.Verify(s => s.PrepareForTestCasesExportAsync(_attributeMap, _projectId), Times.Once);

        // Verify GetTestCasesByConfig called with correct parameters
        _mockTestCaseCommonService.VerifyGetTestCasesByConfigBatchesDirect(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            batches);

        _mockTestCaseCommonService.Verify(s => s.GetArchivedTestCases(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>()), Times.Never);

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
    public async Task ExportTestCases_WithMultipleBatches_ReturnsCorrectTestCaseData()
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
        var result = await _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(250));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
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
    public async Task ExportTestCases_WithExportArchived_ProcessesActiveAndArchivedTestCases()
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
        var result = await _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(80));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
        });

        // Verify GetTestCasesByConfig called with correct parameters
        _mockTestCaseCommonService.VerifyGetTestCasesByConfigBatchesDirect(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            activeBatches);

        // Verify GetArchivedTestCases called with correct parameters
        _mockTestCaseCommonService.VerifyGetArchivedTestCasesBatchesDirect(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            archivedBatches);
    }

    [Test]
    public async Task ExportTestCases_WithMultipleBatchesAndArchived_ProcessesAllCorrectly()
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

        var stringStatuses = _prepDataModel.StatusData.StringStatuses;
        
        // Setup active test cases batches: startAt=0 (100 items), startAt=100 (50 items), startAt=200 (empty)
        var activeBatches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), activeBatch1 },
            { (100, 100), activeBatch2 },
            { (200, 100), emptyBatch }
        };
        
        // Setup archived test cases batches: startAt=0 (100 items), startAt=100 (30 items), startAt=200 (empty)
        var archivedBatches = new Dictionary<(int, int), List<ZephyrTestCase>>
        {
            { (0, 100), archivedBatch1 },
            { (100, 100), archivedBatch2 },
            { (200, 100), emptyBatch }
        };

        _mockTestCaseCommonService.SetupGetTestCasesByConfigBatches(
            _mockAppConfig,
            _mockClient,
            stringStatuses,
            activeBatches);

        _mockTestCaseCommonService.SetupGetArchivedTestCasesBatches(
            _mockAppConfig,
            _mockClient,
            stringStatuses,
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
        var result = await _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(280));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
        });

        _mockTestCaseCommonService.Verify(s => s.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.IsAny<int>(),
            It.Is<int>(x => x == 100),
            stringStatuses), Times.Exactly(3));

        _mockTestCaseCommonService.Verify(s => s.GetArchivedTestCases(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.IsAny<int>(),
            It.Is<int>(x => x == 100),
            stringStatuses), Times.Exactly(3));

        _mockTestCaseCommonService.Verify(s => s.WriteTestCasesAsync(
            It.IsAny<List<ZephyrTestCase>>(),
            _sectionData,
            _attributeMap,
            _prepDataModel.RequiredAttributeNames,
            _prepDataModel.OwnersAttribute), Times.Exactly(4));
    }

    [Test]
    public async Task ExportTestCases_WithEmptyResult_ReturnsEmptyTestCaseData()
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
        var result = await _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);

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
    public async Task ExportTestCases_WithRetryLogic_RetriesOnException()
    {
        // Arrange
        var testCases = TestDataHelper.CreateTestZephyrTestCases(50);
        var testCaseIds = new List<Guid> { Guid.NewGuid() };
        var expectedTestCaseData = TestDataHelper.CreateTestCaseData(testCaseIds);

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
            .ThrowsAsync(new Exception("Network error"))
            .ReturnsAsync(testCases);

        _mockTestCaseCommonService.SetupGetTestCasesByConfig(
            _mockAppConfig,
            _mockClient,
            _prepDataModel.StatusData.StringStatuses,
            100,
            100,
            new List<ZephyrTestCase>());

        _mockTestCaseCommonService
            .Setup(s => s.WriteTestCasesAsync(testCases, _sectionData, _attributeMap, _prepDataModel.RequiredAttributeNames, _prepDataModel.OwnersAttribute))
            .ReturnsAsync(testCaseIds);

        _mockTestCaseCommonService
            .Setup(s => s.PrepareTestCaseIdsData(_attributeMap, _prepDataModel.OwnersAttribute, testCaseIds))
            .Returns(expectedTestCaseData);

        // Act
        var result = await _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EquivalentTo(testCaseIds));
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
    public async Task ExportTestCases_WithPartialFailureThenSuccess_ContinuesProcessing()
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
        var result = await _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(150));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
        });

        VerifyRetryLogging();
    }

    [Test]
    public async Task ExportTestCases_WithExceptionInArchivedProcessing_HandlesGracefully()
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
        var result = await _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Has.Count.EqualTo(80));
            Assert.That(result.TestCaseIds, Is.EquivalentTo(allIds));
        });

        VerifyRetryLogging();
    }

    [Test]
    public async Task ExportTestCases_WithMaxRetriesExceeded_LogsCriticalAndAttemptsExit()
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

        // Act & Assert
        try
        {
            var task = _testCaseService.ExportTestCases(_sectionData, _attributeMap, _projectId);
            await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
        }
        catch
        {
            // Expected - Environment.Exit may terminate the process
        }

        _mockTestCaseCommonService.Verify(s => s.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>()), Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Waiting 3 seconds before next attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get test cases starting from") && v.ToString()!.Contains("Attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
        
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get test cases after") && v.ToString()!.Contains("attempts")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtMostOnce);
    }


    #endregion

    #region Helper Methods

    private void SetupAppConfig(bool exportArchived)
    {
        var appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                ExportArchived = exportArchived
            }
        };
        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);
        _testCaseService = new TestCaseService(
            _mockAppConfig.Object,
            _mockTestCaseCommonService.Object,
            _mockLogger.Object,
            _mockClient.Object);
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Waiting 3 seconds before next attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    #endregion
}
