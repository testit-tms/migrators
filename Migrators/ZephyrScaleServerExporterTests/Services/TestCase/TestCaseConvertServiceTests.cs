using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Helpers;
using ZephyrScaleServerExporter.Services.TestCase;
using ZephyrScaleServerExporter.Services.TestCase.Helpers.Implementations;
using ZephyrScaleServerExporter.Services.TestCase.Implementations;
using ZephyrScaleServerExporterTests.Helpers;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporterTests.Services.TestCase;

[TestFixture]
public class TestCaseConvertServiceTests
{
    private Mock<ILogger<TestCaseConvertService>> _mockLogger;
    private Mock<IClient> _mockClient;
    private Mock<IStepService> _mockStepService;
    private Mock<ITestCaseAttachmentsService> _mockTestCaseAttachmentsService;
    private Mock<ITestCaseAttributesService> _mockTestCaseAttributesService;
    private Mock<ITestCaseAdditionalLinksService> _mockTestCaseAdditionalLinksService;
    private Mock<IParameterService> _mockParameterService;
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<ILogger<TestCaseServiceHelper>> _mockHelperLogger;
    private Mock<IOptions<AppConfig>> _mockAppConfig;
    private TestCaseServiceHelper _testCaseServiceHelper;
    private TestCaseConvertService _testCaseConvertService;

    private Dictionary<string, Attribute> _attributeMap;
    private List<string> _requiredAttributeNames;
    private SectionData _sectionData;
    private Attribute _ownersAttribute;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<TestCaseConvertService>>();
        _mockClient = new Mock<IClient>();
        _mockStepService = new Mock<IStepService>();
        _mockTestCaseAttachmentsService = new Mock<ITestCaseAttachmentsService>();
        _mockTestCaseAttributesService = new Mock<ITestCaseAttributesService>();
        _mockTestCaseAdditionalLinksService = new Mock<ITestCaseAdditionalLinksService>();
        _mockParameterService = new Mock<IParameterService>();
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockHelperLogger = new Mock<ILogger<TestCaseServiceHelper>>();
        _mockAppConfig = new Mock<IOptions<AppConfig>>();

        _testCaseServiceHelper = new TestCaseServiceHelper(
            _mockDetailedLogService.Object,
            _mockHelperLogger.Object);

        _testCaseConvertService = new TestCaseConvertService(
            _mockLogger.Object,
            _mockClient.Object,
            _testCaseServiceHelper,
            _mockStepService.Object,
            _mockTestCaseAttachmentsService.Object,
            _mockTestCaseAttributesService.Object,
            _mockTestCaseAdditionalLinksService.Object,
            _mockParameterService.Object);

        _attributeMap = TestDataHelper.CreateAttributeMap();
        _requiredAttributeNames = new List<string>();
        _sectionData = TestDataHelper.CreateSectionData();
        _ownersAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.OwnerAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };
    }

    #region ConvertTestCase

    [Test]
    public async Task ConvertTestCase_WithFullData_ReturnsCompleteTestCase()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(
            key: "TEST-1",
            name: "Test Case Name",
            description: "Test description",
            status: "Approved",
            priority: "High",
            folder: "Folder1/Folder2",
            ownerKey: "user1");
        zephyrTestCase.Precondition = "Test precondition";
        zephyrTestCase.Labels = new List<string> { "Tag1", "Tag2" };
        zephyrTestCase.TestScript = new ZephyrTestScript
        {
            Id = 1,
            Type = "STEP_BY_STEP",
            Steps = new List<ZephyrStep>
            {
                new ZephyrStep { Description = "Step 1", ExpectedResult = "Result 1", Index = 1 }
            }
        };

        var attributes = new List<CaseAttribute>
        {
            new CaseAttribute { Id = _attributeMap[Constants.IdZephyrAttribute].Id, Value = "TEST-1" },
            new CaseAttribute { Id = _attributeMap[Constants.ZephyrStatusAttribute].Id, Value = "Approved" }
        };

        var stepsData = new StepsData
        {
            Steps = new List<Step>
            {
                new Step
                {
                    Action = "Step 1",
                    Expected = "Result 1",
                    ActionAttachments = new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>(),
                    TestData = string.Empty
                }
            },
            Iterations = new List<Iteration>()
        };

        var links = new List<Link>
        {
            new Link { Title = "Link1", Url = "https://example.com" }
        };

        var attachments = new List<string> { "attachment1.txt" };
        var preconditionAttachments = new List<string> { "precondition_attachment.txt" };

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(stepsData);

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(links);

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(attachments);

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(preconditionAttachments);

        _mockClient
            .Setup(c => c.GetOwner("user1"))
            .ReturnsAsync(TestDataHelper.CreateZephyrOwner("user1", "User One"));

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Test Case Name"));
            Assert.That(result.State, Is.EqualTo(StateType.Ready));
            Assert.That(result.Priority, Is.EqualTo(PriorityType.High));
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.PreconditionSteps, Has.Count.EqualTo(1));
            Assert.That(result.PreconditionSteps[0].Action, Contains.Substring("Test precondition"));
            Assert.That(result.Attributes, Is.EqualTo(attributes));
            Assert.That(result.Links, Is.EqualTo(links));
            Assert.That(result.Attachments, Contains.Item("attachment1.txt"));
            Assert.That(result.Tags, Contains.Item("Tag1"));
            Assert.That(result.Tags, Contains.Item("Tag2"));
            Assert.That(result.Duration, Is.EqualTo(10000));
        });
    }

    [Test]
    public async Task ConvertTestCase_WithNullKey_ReturnsNull()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: null!);
        zephyrTestCase.Key = null;

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.That(result, Is.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping test case")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ConvertTestCase_WithNullFolder_UsesMainFolder()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", folder: null);
        var attributes = new List<CaseAttribute>();

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(new StepsData { Steps = new List<Step>(), Iterations = new List<Iteration>() });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SectionId, Is.EqualTo(_sectionData.SectionMap[Constants.MainFolderKey]));
    }

    [Test]
    public async Task ConvertTestCase_WithNewFolder_CreatesFolder()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", folder: "NewFolder");
        var attributes = new List<CaseAttribute>();

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(new StepsData { Steps = new List<Step>(), Iterations = new List<Iteration>() });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_sectionData.AllSections.ContainsKey(Constants.MainFolderKey + "/NewFolder"), Is.True);
        Assert.That(_sectionData.SectionMap.ContainsKey(Constants.MainFolderKey + "/NewFolder"), Is.True);
    }

    [Test]
    [TestCase("High", PriorityType.High)]
    [TestCase("Normal", PriorityType.Medium)]
    [TestCase("Low", PriorityType.Low)]
    [TestCase("Unknown", PriorityType.Medium)]
    public async Task ConvertTestCase_WithDifferentPriorities_ConvertsCorrectly(string priority, PriorityType expectedPriority)
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", priority: priority);
        var attributes = new List<CaseAttribute>();

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(new StepsData { Steps = new List<Step>(), Iterations = new List<Iteration>() });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.That(result!.Priority, Is.EqualTo(expectedPriority));
    }

    [Test]
    [TestCase("Approved", StateType.Ready)]
    [TestCase("Draft", StateType.NotReady)]
    [TestCase("Deprecated", StateType.NeedsWork)]
    [TestCase("Unknown", StateType.NotReady)]
    public async Task ConvertTestCase_WithDifferentStatuses_ConvertsCorrectly(string status, StateType expectedState)
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", status: status);
        var attributes = new List<CaseAttribute>();

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(new StepsData { Steps = new List<Step>(), Iterations = new List<Iteration>() });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.That(result!.State, Is.EqualTo(expectedState));
    }

    [Test]
    public async Task ConvertTestCase_WithNullPrecondition_DoesNotCreatePreconditionSteps()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        zephyrTestCase.Precondition = null;
        var attributes = new List<CaseAttribute>();

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(new StepsData { Steps = new List<Step>(), Iterations = new List<Iteration>() });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.That(result!.PreconditionSteps, Is.Empty);
    }

    [Test]
    public async Task ConvertTestCase_WithOwner_AddsOwnerToAttributes()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1", ownerKey: "user1");
        var attributes = new List<CaseAttribute>();
        var owner = TestDataHelper.CreateZephyrOwner("user1", "User One");

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(new StepsData { Steps = new List<Step>(), Iterations = new List<Iteration>() });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        _mockClient
            .Setup(c => c.GetOwner("user1"))
            .ReturnsAsync(owner);

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Attributes.Any(a => a.Id == _ownersAttribute.Id && (string)a.Value == "User One"), Is.True);
            Assert.That(_ownersAttribute.Options, Contains.Item("User One"));
        });
    }

    [Test]
    public async Task ConvertTestCase_WithLongTags_ExcludesLongTags()
    {
        // Arrange
        var longTag = new string('A', 31);
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        zephyrTestCase.Labels = new List<string> { "ShortTag", longTag, "AnotherShortTag" };
        var attributes = new List<CaseAttribute>();

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(new StepsData { Steps = new List<Step>(), Iterations = new List<Iteration>() });

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result!.Tags, Contains.Item("ShortTag"));
            Assert.That(result.Tags, Does.Not.Contain((string)longTag));
            Assert.That(result.Tags, Contains.Item("AnotherShortTag"));
        });

        _mockHelperLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("is longer than 30 symbols")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ConvertTestCase_WithDuplicateAttachments_ExcludesDuplicates()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var attributes = new List<CaseAttribute>();
        var stepsData = new StepsData
        {
            Steps = new List<Step>
            {
                new Step
                {
                    Action = "Step",
                    Expected = "Result",
                    ActionAttachments = new List<string> { "duplicate.txt" },
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>(),
                    TestData = string.Empty
                }
            },
            Iterations = new List<Iteration>()
        };

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(new List<Iteration>());

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), It.IsAny<List<Iteration>>()))
            .ReturnsAsync(stepsData);

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string> { "duplicate.txt" });

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.That(result!.Attachments, Has.Count.EqualTo(1));
        Assert.That(result.Attachments, Contains.Item("duplicate.txt"));
    }

    [Test]
    public async Task ConvertTestCase_WithEmptyIterations_SanitizesIterations()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(key: "TEST-1");
        var attributes = new List<CaseAttribute>();
        var stepsData = new StepsData
        {
            Steps = new List<Step>(),
            Iterations = new List<Iteration>
            {
                new Iteration { Parameters = new List<Parameter>() },
                new Iteration { Parameters = new List<Parameter> { new Parameter { Name = "Param1", Value = "Value1" } } }
            }
        };

        _mockTestCaseAttributesService
            .Setup(s => s.CalculateAttributes(zephyrTestCase, _attributeMap, _requiredAttributeNames))
            .Returns(attributes);

        _mockParameterService
            .Setup(s => s.ConvertParameters("TEST-1"))
            .ReturnsAsync(stepsData.Iterations);

        _mockStepService
            .Setup(s => s.ConvertSteps(It.IsAny<Guid>(), It.IsAny<ZephyrTestScript>(), stepsData.Iterations))
            .ReturnsAsync(stepsData);

        _mockTestCaseAdditionalLinksService
            .Setup(s => s.GetAdditionalLinks(zephyrTestCase))
            .ReturnsAsync(new List<Link>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.FillAttachments(It.IsAny<Guid>(), zephyrTestCase, It.IsAny<ZephyrDescriptionData>()))
            .ReturnsAsync(new List<string>());

        _mockTestCaseAttachmentsService
            .Setup(s => s.CalcPreconditionAttachments(It.IsAny<Guid>(), It.IsAny<ZephyrDescriptionData>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _testCaseConvertService.ConvertTestCase(
            zephyrTestCase,
            _sectionData,
            _attributeMap,
            _requiredAttributeNames,
            _ownersAttribute);

        // Assert
        Assert.That(result!.Iterations, Has.Count.EqualTo(1));
        Assert.That(result.Iterations[0].Parameters, Has.Count.EqualTo(1));
    }

    #endregion
}
