using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.AttrubuteMapping;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.TestCase;
using ZephyrScaleServerExporter.Services.TestCase.Implementations;
using ZephyrScaleServerExporterTests.Helpers;
using Attribute = Models.Attribute;
using TestCaseData = ZephyrScaleServerExporter.Models.TestCases.TestCaseData;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporterTests.Services.TestCase;

[TestFixture]
public class TestCaseCommonServiceTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<ILogger<TestCaseCommonService>> _mockLogger;
    private Mock<ITestCaseConvertService> _mockTestCaseConvertService;
    private Mock<ITestCaseAdditionalLinksService> _mockTestCaseAdditionalLinksService;
    private Mock<IStatusService> _mockStatusService;
    private Mock<IMappingConfigReader> _mockMappingConfigReader;
    private Mock<IWriteService> _mockWriteService;
    private Mock<ITestCaseErrorLogService> _mockTestCaseErrorLogService;
    private Mock<IOptions<AppConfig>> _mockAppConfig;
    private Mock<IClient> _mockClient;
    private TestCaseCommonService _testCaseCommonService;

    private Dictionary<string, Attribute> _attributeMap;
    private string _projectId;
    private SectionData _sectionData;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockLogger = new Mock<ILogger<TestCaseCommonService>>();
        _mockTestCaseConvertService = new Mock<ITestCaseConvertService>();
        _mockTestCaseAdditionalLinksService = new Mock<ITestCaseAdditionalLinksService>();
        _mockStatusService = new Mock<IStatusService>();
        _mockMappingConfigReader = new Mock<IMappingConfigReader>();
        _mockWriteService = new Mock<IWriteService>();
        _mockTestCaseErrorLogService = new Mock<ITestCaseErrorLogService>();
        _mockAppConfig = new Mock<IOptions<AppConfig>>();
        _mockClient = new Mock<IClient>();

        _testCaseCommonService = new TestCaseCommonService(
            _mockDetailedLogService.Object,
            _mockLogger.Object,
            _mockTestCaseConvertService.Object,
            _mockTestCaseAdditionalLinksService.Object,
            _mockStatusService.Object,
            _mockMappingConfigReader.Object,
            _mockWriteService.Object,
            _mockTestCaseErrorLogService.Object);

        _attributeMap = TestDataHelper.CreateAttributeMap();
        _projectId = TestDataHelper.GenerateProjectId().ToString();
        _sectionData = TestDataHelper.CreateSectionData();

        var appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                FilterName = string.Empty,
                FilterSection = string.Empty
            }
        };
        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);
    }

    #region PrepareForTestCasesExportAsync

    [Test]
    public async Task PrepareForTestCasesExportAsync_WithFullData_ReturnsCorrectModel()
    {
        // Arrange
        var attributeMap = new Dictionary<string, Attribute>(_attributeMap);
        attributeMap.Remove(Constants.ZephyrStatusAttribute);

        var statusData = TestDataHelper.CreateStatusData();
        _mockStatusService
            .Setup(s => s.ConvertStatuses(_projectId))
            .ReturnsAsync(statusData);

        // Act
        var result = await _testCaseCommonService.PrepareForTestCasesExportAsync(attributeMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.OwnersAttribute, Is.Not.Null);
            Assert.That(result.OwnersAttribute.Name, Is.EqualTo(Constants.OwnerAttribute));
            Assert.That(result.OwnersAttribute.Type, Is.EqualTo(AttributeType.Options));
            Assert.That(result.OwnersAttribute.IsRequired, Is.False);
            Assert.That(result.StatusData, Is.EqualTo(statusData));
            Assert.That(result.RequiredAttributeNames, Is.Not.Null);
            Assert.That(attributeMap.ContainsKey(statusData.StatusAttribute.Name), Is.True);
        });

        _mockStatusService.Verify(s => s.ConvertStatuses(_projectId), Times.Once);

        _mockDetailedLogService.Verify(s => s.LogInformation(
            It.Is<string>(msg => msg.Contains("Get all attribute values")),
            It.IsAny<object[]>()), Times.AtLeastOnce);

        _mockDetailedLogService.Verify(s => s.LogInformation(
            It.Is<string>(msg => msg.Contains("Get all required attributes")),
            It.IsAny<object[]>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task PrepareForTestCasesExportAsync_WithEmptyAttributeMap_ReturnsModelWithEmptyRequiredAttributes()
    {
        // Arrange
        var emptyMap = new Dictionary<string, Attribute>();

        var statusData = TestDataHelper.CreateStatusData();
        _mockStatusService
            .Setup(s => s.ConvertStatuses(_projectId))
            .ReturnsAsync(statusData);

        // Act
        var result = await _testCaseCommonService.PrepareForTestCasesExportAsync(emptyMap, _projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.RequiredAttributeNames, Is.Empty);
            Assert.That(emptyMap.ContainsKey(statusData.StatusAttribute.Name), Is.True);
        });
    }

    [Test]
    public async Task PrepareForTestCasesExportAsync_WithNoRequiredAttributes_ReturnsEmptyRequiredList()
    {
        // Arrange
        var mapWithoutRequired = new Dictionary<string, Attribute>
        {
            {
                Constants.ComponentAttribute,
                new Attribute
                {
                    Id = Guid.NewGuid(),
                    Name = Constants.ComponentAttribute,
                    Type = AttributeType.Options,
                    IsRequired = false,
                    IsActive = true,
                    Options = new List<string>()
                }
            }
        };

        var statusData = TestDataHelper.CreateStatusData();
        _mockStatusService
            .Setup(s => s.ConvertStatuses(_projectId))
            .ReturnsAsync(statusData);

        // Act
        var result = await _testCaseCommonService.PrepareForTestCasesExportAsync(mapWithoutRequired, _projectId);

        // Assert
        Assert.That(result.RequiredAttributeNames, Is.Empty);
    }

    [Test]
    public void PrepareForTestCasesExportAsync_WithStatusServiceException_PropagatesException()
    {
        // Arrange
        var exception = new Exception("Status service error");
        _mockStatusService
            .Setup(s => s.ConvertStatuses(_projectId))
            .ThrowsAsync(exception);

        // Act & Assert
        Assert.That(
            async () => await _testCaseCommonService.PrepareForTestCasesExportAsync(_attributeMap, _projectId),
            Throws.Exception.EqualTo(exception));
    }

    #endregion

    #region PrepareTestCaseIdsData

    [Test]
    public void PrepareTestCaseIdsData_WithOwnersAttributeWithOptions_ReturnsDataWithOwnersAttribute()
    {
        // Arrange
        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string> { "User1", "User2" }
        };

        var testCaseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseCommonService.PrepareTestCaseIdsData(_attributeMap, ownersAttribute, testCaseIds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EqualTo(testCaseIds));
            Assert.That(result.Attributes, Is.Not.Null);
            Assert.That(result.Attributes, Contains.Item(ownersAttribute));
            Assert.That(result.Attributes.Count, Is.GreaterThan(_attributeMap.Count));
        });

        _mockMappingConfigReader.Verify(m => m.InitOnce("mapping.json", ""), Times.AtLeastOnce);
    }

    [Test]
    public void PrepareTestCaseIdsData_WithOwnersAttributeWithoutOptions_ReturnsDataWithoutOwnersAttribute()
    {
        // Arrange
        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        var testCaseIds = new List<Guid> { Guid.NewGuid() };

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseCommonService.PrepareTestCaseIdsData(_attributeMap, ownersAttribute, testCaseIds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestCaseIds, Is.EqualTo(testCaseIds));
            Assert.That(result.Attributes, Does.Not.Contain(ownersAttribute));
            Assert.That(result.Attributes.Count, Is.EqualTo(_attributeMap.Count));
        });
    }

    [Test]
    public void PrepareTestCaseIdsData_WithStatusAttributeMapping_RemapsOptions()
    {
        // Arrange
        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        var testCaseIds = new List<Guid> { Guid.NewGuid() };

        var statusAttribute = _attributeMap[Constants.ZephyrStatusAttribute];

        statusAttribute.Options = new List<string> { "Approved", "Draft" };

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue("Approved", "Состояние"))
            .Returns("Ready");

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue("Draft", "Состояние"))
            .Returns("NotReady");

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.Is<string>(s => s != "Approved" && s != "Draft"), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseCommonService.PrepareTestCaseIdsData(_attributeMap, ownersAttribute, testCaseIds);

        // Assert
        var resultStatusAttribute = result.Attributes.FirstOrDefault(a => a.Name == Constants.ZephyrStatusAttribute);
        Assert.Multiple(() =>
        {
            Assert.That(resultStatusAttribute, Is.Not.Null);
            Assert.That(resultStatusAttribute!.Options, Contains.Item("Ready"));
            Assert.That(resultStatusAttribute.Options, Contains.Item("NotReady"));
            Assert.That(resultStatusAttribute.Options, Has.Count.EqualTo(2));
        });

        _mockDetailedLogService.Verify(s => s.LogInformation(
            It.Is<string>(msg => msg.Contains("Map") && msg.Contains("to")),
            It.IsAny<object[]>()), Times.AtLeastOnce);
    }

    [Test]
    public void PrepareTestCaseIdsData_WithMappingConfigReaderException_HandlesGracefully()
    {
        // Arrange
        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        var testCaseIds = new List<Guid> { Guid.NewGuid() };

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Mapping error"));

        // Act
        var result = _testCaseCommonService.PrepareTestCaseIdsData(_attributeMap, ownersAttribute, testCaseIds);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error while mapping attribute value")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region WriteTestCasesAsync

    [Test]
    public async Task WriteTestCasesAsync_WithMultipleTestCases_ProcessesAllSuccessfully()
    {
        // Arrange
        var testCases = TestDataHelper.CreateTestZephyrTestCases(3);

        var requiredAttributeNames = new List<string> { "RequiredAttribute" };

        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        var expectedIds = testCases.Select(_ => Guid.NewGuid()).ToList();

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(testCases[0], _sectionData, _attributeMap, requiredAttributeNames, ownersAttribute))
            .ReturnsAsync(new global::Models.TestCase
            {
                Id = expectedIds[0],
                Name = testCases[0].Name,
                SectionId = _sectionData.MainSection.Id,
                Links = new List<Link>()
            });

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(testCases[1], _sectionData, _attributeMap, requiredAttributeNames, ownersAttribute))
            .ReturnsAsync(new global::Models.TestCase
            {
                Id = expectedIds[1],
                Name = testCases[1].Name,
                SectionId = _sectionData.MainSection.Id,
                Links = new List<Link>()
            });

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(testCases[2], _sectionData, _attributeMap, requiredAttributeNames, ownersAttribute))
            .ReturnsAsync(new global::Models.TestCase
            {
                Id = expectedIds[2],
                Name = testCases[2].Name,
                SectionId = _sectionData.MainSection.Id,
                Links = new List<Link>()
            });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.ConvertIssueLinkByIssueId(It.IsAny<string>()))
            .ReturnsAsync(new Link { Title = "Test", Url = "https://test.com" });

        // Act
        var result = await _testCaseCommonService.WriteTestCasesAsync(
            testCases,
            _sectionData,
            _attributeMap,
            requiredAttributeNames,
            ownersAttribute);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result, Is.EquivalentTo(expectedIds));
        });

        _mockTestCaseConvertService.Verify(s => s.ConvertTestCase(
            It.IsAny<ZephyrTestCase>(),
            _sectionData,
            _attributeMap,
            requiredAttributeNames,
            ownersAttribute), Times.Exactly(3));

        _mockWriteService.Verify(s => s.WriteTestCase(It.IsAny<global::Models.TestCase>()), Times.Exactly(3));
    }

    [Test]
    public async Task WriteTestCasesAsync_WithSingleTestCase_ProcessesSuccessfully()
    {
        // Arrange
        var testCase = TestDataHelper.CreateZephyrTestCase();
        var testCases = new List<ZephyrTestCase> { testCase };
        var requiredAttributeNames = new List<string>();

        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        var expectedId = Guid.NewGuid();

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(
                testCase,
                _sectionData,
                _attributeMap,
                requiredAttributeNames,
                ownersAttribute))
            .ReturnsAsync(new global::Models.TestCase
            {
                Id = expectedId,
                Name = testCase.Name,
                SectionId = _sectionData.MainSection.Id,
                Links = new List<Link>()
            });

        // Act
        var result = await _testCaseCommonService.WriteTestCasesAsync(
            testCases,
            _sectionData,
            _attributeMap,
            requiredAttributeNames,
            ownersAttribute);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(expectedId));
        });
    }

    [Test]
    public async Task WriteTestCasesAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var testCases = new List<ZephyrTestCase>();
        var requiredAttributeNames = new List<string>();
        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        // Act
        var result = await _testCaseCommonService.WriteTestCasesAsync(
            testCases,
            _sectionData,
            _attributeMap,
            requiredAttributeNames,
            ownersAttribute);

        // Assert
        Assert.That(result, Is.Empty);

        _mockTestCaseConvertService.Verify(s => s.ConvertTestCase(
            It.IsAny<ZephyrTestCase>(),
            It.IsAny<SectionData>(),
            It.IsAny<Dictionary<string, Attribute>>(),
            It.IsAny<List<string>>(),
            It.IsAny<Attribute>()), Times.Never);
    }

    [Test]
    public async Task WriteTestCasesAsync_WithNullConvertResult_SkipsTestCase()
    {
        // Arrange
        var testCase = TestDataHelper.CreateZephyrTestCase();
        var testCases = new List<ZephyrTestCase> { testCase };
        var requiredAttributeNames = new List<string>();
        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(
                testCase,
                _sectionData,
                _attributeMap,
                requiredAttributeNames,
                ownersAttribute))
            .ReturnsAsync((global::Models.TestCase?)null);

        // Act
        var result = await _testCaseCommonService.WriteTestCasesAsync(
            testCases,
            _sectionData,
            _attributeMap,
            requiredAttributeNames,
            ownersAttribute);

        // Assert
        Assert.That(result, Is.Empty);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Conversion of ZephyrTestCase") && v.ToString()!.Contains("resulted in null")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task WriteTestCasesAsync_WithExceptionInConvertAndWrite_LogsErrorAndReturnsEmptyList()
    {
        // Arrange
        var testCase = TestDataHelper.CreateZephyrTestCase();
        var testCases = new List<ZephyrTestCase> { testCase };
        var requiredAttributeNames = new List<string>();
        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };
        var exception = new Exception("Convert error");

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(
                testCase,
                _sectionData,
                _attributeMap,
                requiredAttributeNames,
                ownersAttribute))
            .ThrowsAsync(exception);

        // Act
        var result = await _testCaseCommonService.WriteTestCasesAsync(
            testCases,
            _sectionData,
            _attributeMap,
            requiredAttributeNames,
            ownersAttribute);

        // Assert
        Assert.That(result, Is.Empty);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error during ConvertAndWriteCaseAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockTestCaseErrorLogService.Verify(s => s.LogError(
            exception,
            It.Is<string>(msg => msg.Contains("Error processing individual test case")),
            testCase,
            null), Times.Once);
    }

    [Test]
    public async Task WriteTestCasesAsync_WithExceptionInTaskWhenAll_LogsErrorAndReturnsPartialResults()
    {
        // Arrange
        var testCase1 = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var testCase2 = TestDataHelper.CreateZephyrTestCase(key: "TEST-2");
        var testCases = new List<ZephyrTestCase> { testCase1, testCase2 };
        var requiredAttributeNames = new List<string>();

        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        var expectedId = Guid.NewGuid();

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(
                testCase1,
                _sectionData,
                _attributeMap,
                requiredAttributeNames,
                ownersAttribute))
            .ReturnsAsync(new global::Models.TestCase
            {
                Id = expectedId,
                Name = testCase1.Name,
                SectionId = _sectionData.MainSection.Id,
                Links = new List<Link>()
            });

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(
                testCase2,
                _sectionData,
                _attributeMap,
                requiredAttributeNames,
                ownersAttribute))
            .ThrowsAsync(new Exception("Error in second case"));

        // Act
        var result = await _testCaseCommonService.WriteTestCasesAsync(
            testCases,
            _sectionData,
            _attributeMap,
            requiredAttributeNames,
            ownersAttribute);

        // Assert
        Assert.That(result, Is.Empty);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error while writing test cases batch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockTestCaseErrorLogService.Verify(s => s.LogError(
            It.IsAny<Exception>(),
            It.Is<string>(msg => msg.Contains("An error occurred during Task.WhenAll")),
            null,
            testCases), Times.Once);
    }

    [Test]
    public async Task WriteTestCasesAsync_WithIncorrectIssueLink_FixesLink()
    {
        // Arrange
        var testCase = TestDataHelper.CreateZephyrTestCase();
        var testCases = new List<ZephyrTestCase> { testCase };
        var requiredAttributeNames = new List<string>();

        var ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        var expectedId = Guid.NewGuid();
        var incorrectLink = new Link { Title = "Test", Url = "https://jira.com/rest/api/2/issue/10001" };
        var fixedLink = new Link { Title = "Fixed Issue", Url = "https://jira.com/browse/TEST-123" };

        _mockTestCaseConvertService
            .Setup(s => s.ConvertTestCase(
                testCase,
                _sectionData,
                _attributeMap,
                requiredAttributeNames,
                ownersAttribute))
            .ReturnsAsync(new global::Models.TestCase
            {
                Id = expectedId,
                Name = testCase.Name,
                SectionId = _sectionData.MainSection.Id,
                Links = new List<Link> { incorrectLink }
            });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.ConvertIssueLinkByIssueId("10001"))
            .ReturnsAsync(fixedLink);

        // Act
        var result = await _testCaseCommonService.WriteTestCasesAsync(
            testCases,
            _sectionData,
            _attributeMap,
            requiredAttributeNames,
            ownersAttribute);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));

        _mockTestCaseAdditionalLinksService.Verify(s => s.ConvertIssueLinkByIssueId("10001"), Times.Once);

        _mockWriteService.Verify(s => s.WriteTestCase(
            It.Is<global::Models.TestCase>(tc => tc.Links[0].Url == fixedLink.Url)), Times.Once);
    }

    #endregion

    #region GetTestCasesByConfig

    [Test]
    public async Task GetTestCasesByConfig_WithFilterNameAndFilterSection_CallsGetTestCasesWithFilter()
    {
        // Arrange
        var appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                FilterName = "TestName",
                FilterSection = "123"
            }
        };

        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);

        var expectedTestCases = TestDataHelper.CreateTestZephyrTestCases(5);
        var statuses = "\"Approved\",\"Draft\"";

        _mockClient
            .Setup(c => c.GetTestCasesWithFilter(0, 100, statuses, It.Is<string>(f => f.Contains("TestName") && f.Contains("123"))))
            .ReturnsAsync(expectedTestCases);

        // Act
        var result = await _testCaseCommonService.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedTestCases));
            Assert.That(result, Has.Count.EqualTo(5));
        });

        _mockClient.Verify(c => c.GetTestCasesWithFilter(
            0,
            100,
            statuses,
            It.Is<string>(f => f.Contains("testCase.name LIKE \"TestName\"") && f.Contains("testCase.folderTreeId IN (123)"))), Times.Once);

        _mockClient.Verify(c => c.GetTestCases(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetTestCasesByConfig_WithoutFilters_CallsGetTestCases()
    {
        // Arrange
        var appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                FilterName = string.Empty,
                FilterSection = string.Empty
            }
        };

        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);

        var expectedTestCases = TestDataHelper.CreateTestZephyrTestCases(3);
        var statuses = "\"Approved\"";

        _mockClient
            .Setup(c => c.GetTestCases(0, 100, statuses))
            .ReturnsAsync(expectedTestCases);

        // Act
        var result = await _testCaseCommonService.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.That(result, Is.EqualTo(expectedTestCases));
        _mockClient.Verify(c => c.GetTestCases(0, 100, statuses), Times.Once);
        _mockClient.Verify(c => c.GetTestCasesWithFilter(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    [TestCase("", "section123")]
    [TestCase("TestName", "")]
    [TestCase("TestName", "section123")]
    public async Task GetTestCasesByConfig_WithDifferentFilterCombinations_BuildsCorrectFilter(string filterName, string filterSection)
    {
        // Arrange
        var appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                FilterName = filterName,
                FilterSection = filterSection
            }
        };

        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);

        var expectedTestCases = TestDataHelper.CreateTestZephyrTestCases(2);
        var statuses = "\"Approved\"";

        if (string.IsNullOrEmpty(filterName) && string.IsNullOrEmpty(filterSection))
        {
            _mockClient
                .Setup(c => c.GetTestCases(0, 100, statuses))
                .ReturnsAsync(expectedTestCases);
        }
        else
        {
            _mockClient
                .Setup(c => c.GetTestCasesWithFilter(0, 100, statuses, It.IsAny<string>()))
                .ReturnsAsync(expectedTestCases);
        }

        // Act
        var result = await _testCaseCommonService.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.That(result, Is.EqualTo(expectedTestCases));
        if (string.IsNullOrEmpty(filterName) && string.IsNullOrEmpty(filterSection))
        {
            _mockClient.Verify(c => c.GetTestCases(0, 100, statuses), Times.Once);
        }
        else
        {
            _mockClient.Verify(c => c.GetTestCasesWithFilter(
                0,
                100,
                statuses,
                It.Is<string>(f =>
                    (string.IsNullOrEmpty(filterName) || f.Contains($"testCase.name LIKE \"{filterName}\"")) &&
                    (string.IsNullOrEmpty(filterSection) || f.Contains($"testCase.folderTreeId IN ({filterSection})")))), Times.Once);
        }
    }

    [Test]
    public async Task GetTestCasesByConfig_WithOnlyFilterName_BuildsNameFilter()
    {
        // Arrange
        var appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                FilterName = "MyTest",
                FilterSection = string.Empty
            }
        };

        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);

        var expectedTestCases = TestDataHelper.CreateTestZephyrTestCases(1);
        var statuses = "\"Approved\"";

        _mockClient
            .Setup(c => c.GetTestCasesWithFilter(0, 100, statuses, It.Is<string>(f => f.Contains("MyTest") && !f.Contains("folderTreeId"))))
            .ReturnsAsync(expectedTestCases);

        // Act
        var result = await _testCaseCommonService.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.That(result, Is.EqualTo(expectedTestCases));
        _mockClient.Verify(c => c.GetTestCasesWithFilter(
            0,
            100,
            statuses,
            It.Is<string>(f => f.Contains("testCase.name LIKE \"MyTest\""))), Times.Once);
    }

    [Test]
    public async Task GetTestCasesByConfig_WithOnlyFilterSection_BuildsSectionFilter()
    {
        // Arrange
        var appConfig = new AppConfig
        {
            ResultPath = "test_path",
            Zephyr = new ZephyrConfig
            {
                FilterName = string.Empty,
                FilterSection = "456"
            }
        };

        _mockAppConfig.Setup(c => c.Value).Returns(appConfig);

        var expectedTestCases = TestDataHelper.CreateTestZephyrTestCases(1);
        var statuses = "\"Approved\"";

        _mockClient
            .Setup(c => c.GetTestCasesWithFilter(0, 100, statuses, It.Is<string>(f => f.Contains("456") && !f.Contains("LIKE"))))
            .ReturnsAsync(expectedTestCases);

        // Act
        var result = await _testCaseCommonService.GetTestCasesByConfig(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.That(result, Is.EqualTo(expectedTestCases));
        _mockClient.Verify(c => c.GetTestCasesWithFilter(
            0,
            100,
            statuses,
            It.Is<string>(f => f.Contains("testCase.folderTreeId IN (456)"))), Times.Once);
    }

    #endregion

    #region GetArchivedTestCases

    [Test]
    public async Task GetArchivedTestCases_WithTestCases_AddsArchivedMetadata()
    {
        // Arrange
        var archivedTestCases = TestDataHelper.CreateTestZephyrTestCases(3);
        var statuses = "\"Approved\"";

        _mockClient
            .Setup(c => c.GetTestCasesArchived(0, 100, statuses))
            .ReturnsAsync(archivedTestCases);

        // Act
        var result = await _testCaseCommonService.GetArchivedTestCases(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result.All(tc => tc.IsArchived), Is.True);
            Assert.That(result.All(tc => tc.Labels != null && tc.Labels.Contains("Archived")), Is.True);
            Assert.That(result.All(tc => tc.CustomFields != null && tc.CustomFields.ContainsKey("Archived") && tc.CustomFields["Archived"].ToString() == "true"), Is.True);
        });

        _mockClient.Verify(c => c.GetTestCasesArchived(0, 100, statuses), Times.Once);
    }

    [Test]
    public async Task GetArchivedTestCases_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<ZephyrTestCase>();
        var statuses = "\"Approved\"";

        _mockClient
            .Setup(c => c.GetTestCasesArchived(0, 100, statuses))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _testCaseCommonService.GetArchivedTestCases(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetArchivedTestCases_WithExistingLabelsAndCustomFields_AddsToExisting()
    {
        // Arrange
        var testCase = TestDataHelper.CreateZephyrTestCase();
        testCase.Labels = new List<string> { "ExistingLabel" };
        testCase.CustomFields = new Dictionary<string, object> { { "ExistingField", "value" } };
        var archivedTestCases = new List<ZephyrTestCase> { testCase };
        var statuses = "\"Approved\"";

        _mockClient
            .Setup(c => c.GetTestCasesArchived(0, 100, statuses))
            .ReturnsAsync(archivedTestCases);

        // Act
        var result = await _testCaseCommonService.GetArchivedTestCases(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Labels, Contains.Item("ExistingLabel"));
            Assert.That(result[0].Labels, Contains.Item("Archived"));
            Assert.That(result[0].CustomFields!.ContainsKey("ExistingField"), Is.True);
            Assert.That(result[0].CustomFields!.ContainsKey("Archived"), Is.True);
            Assert.That(result[0].CustomFields!["Archived"]!.ToString(), Is.EqualTo("true"));
        });
    }

    [Test]
    public async Task GetArchivedTestCases_WithNullLabelsAndCustomFields_InitializesAndAdds()
    {
        // Arrange
        var testCase = TestDataHelper.CreateZephyrTestCase();
        testCase.Labels = null;
        testCase.CustomFields = null;
        var archivedTestCases = new List<ZephyrTestCase> { testCase };
        var statuses = "\"Approved\"";

        _mockClient
            .Setup(c => c.GetTestCasesArchived(0, 100, statuses))
            .ReturnsAsync(archivedTestCases);

        // Act
        var result = await _testCaseCommonService.GetArchivedTestCases(
            _mockAppConfig.Object,
            _mockClient.Object,
            0,
            100,
            statuses);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Labels, Is.Not.Null);
            Assert.That(result[0].Labels, Contains.Item("Archived"));
            Assert.That(result[0].CustomFields, Is.Not.Null);
            Assert.That(result[0].CustomFields!.ContainsKey("Archived"), Is.True);
            Assert.That(result[0].IsArchived, Is.True);
        });
    }

    [Test]
    public async Task GetArchivedTestCases_VerifiesCorrectParameters()
    {
        // Arrange
        var archivedTestCases = TestDataHelper.CreateTestZephyrTestCases(2);
        var statuses = "\"Approved\",\"Draft\"";
        var startAt = 50;
        var maxResults = 200;

        _mockClient
            .Setup(c => c.GetTestCasesArchived(startAt, maxResults, statuses))
            .ReturnsAsync(archivedTestCases);

        // Act
        var result = await _testCaseCommonService.GetArchivedTestCases(
            _mockAppConfig.Object,
            _mockClient.Object,
            startAt,
            maxResults,
            statuses);

        // Assert
        Assert.That(result, Is.EqualTo(archivedTestCases));
        _mockClient.Verify(c => c.GetTestCasesArchived(startAt, maxResults, statuses), Times.Once);
    }

    #endregion
}
