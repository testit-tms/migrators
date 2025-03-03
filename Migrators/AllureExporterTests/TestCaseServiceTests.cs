using AllureExporter.Client;
using AllureExporter.Models.Config;
using AllureExporter.Models.Project;
using AllureExporter.Models.Relation;
using AllureExporter.Models.Step;
using AllureExporter.Models.TestCase;
using AllureExporter.Services;
using AllureExporter.Services.Implementations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Moq;
using Constants = AllureExporter.Models.Project.Constants;

namespace AllureExporterTests;

public class TestCaseServiceTests
{
    private Mock<ILogger<TestCaseService>> _logger;
    private Mock<IClient> _client;
    private Mock<IAttachmentService> _attachmentService;
    private Mock<IStepService> _stepService;
    private Mock<IOptions<AppConfig>> _config;
    private TestCaseService _sut;
    private const long ProjectId = 1;
    private const long TestCaseId = 100;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<TestCaseService>>();
        _client = new Mock<IClient>();
        _attachmentService = new Mock<IAttachmentService>();
        _stepService = new Mock<IStepService>();
        _config = new Mock<IOptions<AppConfig>>();

        _config.Setup(x => x.Value).Returns(new AppConfig
        {
            Allure = new AllureConfig
            {
                Url = "http://test.allure.com"
            }
        });

        // Setup default returns for methods that should never return null
        _client.Setup(x => x.GetTestCaseIdsFromMainSuite(It.IsAny<long>()))
            .ReturnsAsync(new List<long>());
        _client.Setup(x => x.GetTestCaseIdsFromSuite(It.IsAny<long>(), It.IsAny<long>()))
            .ReturnsAsync(new List<long>());
        _client.Setup(x => x.GetIssueLinks(It.IsAny<long>()))
            .ReturnsAsync(new List<AllureLink>());
        _client.Setup(x => x.GetRelations(It.IsAny<long>()))
            .ReturnsAsync(new List<AllureRelation>());
        _client.Setup(x => x.GetCustomFieldsFromTestCase(It.IsAny<long>()))
            .ReturnsAsync(new List<AllureCustomField>());

        _sut = new TestCaseService(
            _logger.Object,
            _client.Object,
            _attachmentService.Object,
            _stepService.Object,
            _config.Object);
    }

    [Test]
    public async Task ConvertTestCases_Success()
    {
        // Arrange
        var mainSectionId = Guid.NewGuid();
        var regularSectionId = Guid.NewGuid();
        var sectionInfo = new SectionInfo
        {
            MainSection = new Section
            {
                Id = mainSectionId,
                Name = "Main Section",
                Sections = new List<Section>()
            },
            SectionDictionary = new Dictionary<long, Guid>
            {
                { Constants.MainSectionId, mainSectionId }
            }
        };

        var sharedStepMap = new Dictionary<string, Guid>
        {
            { "1", Guid.NewGuid() }
        };

        var featureAttributeId = Guid.NewGuid();
        var storyAttributeId = Guid.NewGuid();
        var attributes = new Dictionary<string, Guid>
        {
            { Constants.AllureStatus, Guid.NewGuid() },
            { Constants.AllureTestLayer, Guid.NewGuid() },
            { Constants.Feature, featureAttributeId },
            { Constants.Story, storyAttributeId }
        };

        var testCaseIds = new List<long> { TestCaseId };
        var testCase = new AllureTestCase
        {
            Id = TestCaseId,
            Name = "Test Case",
            Description = "Description",
            Status = new Status { Name = "Active" },
            Layer = new TestLayer { Name = "UI" },
            Tags = new List<Tag> { new() { Name = "Tag1" } },
            Links = new List<TestCaseLink> { new() { Name = "Link1", Url = "http://test.com" } }
        };

        var issueLinks = new List<AllureLink>
        {
            new() { Name = "Issue1", Url = "http://issues.com/1" }
        };

        var relations = new List<AllureRelation>
        {
            new()
            {
                Id = 1,
                Type = "related to",
                Target = new AllureRelationTarget { Id = 2, Name = "Related Test" }
            }
        };

        var customFields = new List<AllureCustomField>
        {
            new() 
            { 
                CustomField = new CustomField { Name = Constants.Feature },
                Name = "Feature1"
            },
            new() 
            { 
                CustomField = new CustomField { Name = Constants.Story },
                Name = "Story1"
            }
        };

        var steps = new List<Step>
        {
            new() { Action = "Step 1" }
        };

        var attachments = new List<string> { "attachment1.png" };

        _client.Setup(x => x.GetTestCaseIdsFromMainSuite(ProjectId)).ReturnsAsync(testCaseIds);
        _client.Setup(x => x.GetTestCaseById(TestCaseId)).ReturnsAsync(testCase);
        _client.Setup(x => x.GetIssueLinks(TestCaseId)).ReturnsAsync(issueLinks);
        _client.Setup(x => x.GetRelations(TestCaseId)).ReturnsAsync(relations);
        _client.Setup(x => x.GetCustomFieldsFromTestCase(TestCaseId)).ReturnsAsync(customFields);
        _stepService.Setup(x => x.ConvertStepsForTestCase(TestCaseId, sharedStepMap)).ReturnsAsync(steps);
        _attachmentService.Setup(x => x.DownloadAttachmentsforTestCase(TestCaseId, It.IsAny<Guid>()))
            .ReturnsAsync(attachments);

        // Act
        var result = await _sut.ConvertTestCases(ProjectId, sharedStepMap, attributes, sectionInfo);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1), "Should have one test case from main suite");
            var convertedTestCase = result[0];

            Assert.That(convertedTestCase.Name, Is.EqualTo(testCase.Name), "Test case name should match");
            Assert.That(convertedTestCase.Description, Is.EqualTo(testCase.Description), "Description should match");
            Assert.That(convertedTestCase.State, Is.EqualTo(StateType.NotReady), "State should be NotReady");
            Assert.That(convertedTestCase.Priority, Is.EqualTo(PriorityType.Medium), "Priority should be Medium");
            Assert.That(convertedTestCase.Tags, Is.EquivalentTo(testCase.Tags.Select(t => t.Name)), "Tags should match");
            Assert.That(convertedTestCase.Steps, Is.EqualTo(steps), "Steps should match");
            Assert.That(convertedTestCase.Attachments, Is.EqualTo(attachments), "Attachments should match");
            Assert.That(convertedTestCase.Links, Has.Count.EqualTo(3), "Should have 3 links (Regular + Issue + Relation)");
            
            // Verify section structure
            var featureSection = sectionInfo.MainSection.Sections.FirstOrDefault(s => s.Name == "Feature1");
            Assert.That(featureSection, Is.Not.Null, "Feature section should exist");
            Assert.That(featureSection!.Sections, Is.Not.Null, "Feature section should have Sections collection");
            
            var storySection = featureSection.Sections.FirstOrDefault(s => s.Name == "Story1");
            Assert.That(storySection, Is.Not.Null, "Story section should exist");

            _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);

            Assert.That(convertedTestCase.SectionId, Is.EqualTo(storySection!.Id), 
                $"Test case should be in story section. Expected: {storySection.Id}, Actual: {convertedTestCase.SectionId}");
        });

        // Verify service calls
        _client.Verify(x => x.GetTestCaseIdsFromMainSuite(ProjectId), Times.Once);
        _client.Verify(x => x.GetTestCaseById(TestCaseId), Times.Once);
        _client.Verify(x => x.GetIssueLinks(TestCaseId), Times.Once);
        _client.Verify(x => x.GetRelations(TestCaseId), Times.Once);
        _client.Verify(x => x.GetCustomFieldsFromTestCase(TestCaseId), Times.Once);
        _stepService.Verify(x => x.ConvertStepsForTestCase(TestCaseId, sharedStepMap), Times.Once);
        _attachmentService.Verify(x => x.DownloadAttachmentsforTestCase(TestCaseId, It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task ConvertTestCases_WithLongName_CutsName()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var sectionInfo = new SectionInfo
        {
            MainSection = new Section
            {
                Id = sectionId,
                Sections = new List<Section>()
            },
            SectionDictionary = new Dictionary<long, Guid> { { Constants.MainSectionId, sectionId } }
        };

        var longName = new string('x', 300);
        var testCase = new AllureTestCase
        {
            Id = TestCaseId,
            Name = longName,
            Status = new Status { Name = "Active" }
        };

        _client.Setup(x => x.GetTestCaseIdsFromMainSuite(ProjectId)).ReturnsAsync(new List<long> { TestCaseId });
        _client.Setup(x => x.GetTestCaseById(TestCaseId)).ReturnsAsync(testCase);
        _client.Setup(x => x.GetCustomFieldsFromTestCase(TestCaseId))
            .ReturnsAsync(new List<AllureCustomField>
            {
                new()
                {
                    CustomField = new CustomField { Name = Constants.Feature },
                    Name = string.Empty
                },
                new()
                {
                    CustomField = new CustomField { Name = Constants.Story },
                    Name = string.Empty
                }
            });

        // Act
        var result = await _sut.ConvertTestCases(
            ProjectId,
            new Dictionary<string, Guid>(),
            new Dictionary<string, Guid>
            {
                { Constants.AllureStatus, Guid.NewGuid() },
                { Constants.AllureTestLayer, Guid.NewGuid() },
                { Constants.Feature, Guid.NewGuid() },
                { Constants.Story, Guid.NewGuid() }
            },
            sectionInfo);

        // Assert
        Assert.That(result[0].Name, Has.Length.LessThanOrEqualTo(255));
        Assert.That(result[0].Name, Does.StartWith("[CUT] "));
        Assert.That(result[0].Name, Does.EndWith("..."));
    }

    [Test]
    public async Task ConvertTestCases_WithFeatureAndStory_CreatesNestedSections()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var sectionInfo = new SectionInfo
        {
            MainSection = new Section
            {
                Id = sectionId,
                Sections = new List<Section>() // Initialize Sections list
            },
            SectionDictionary = new Dictionary<long, Guid> { { Constants.MainSectionId, sectionId } }
        };

        var featureAttributeId = Guid.NewGuid();
        var storyAttributeId = Guid.NewGuid();
        var attributes = new Dictionary<string, Guid>
        {
            { Constants.AllureStatus, Guid.NewGuid() },
            { Constants.AllureTestLayer, Guid.NewGuid() },
            { Constants.Feature, featureAttributeId },
            { Constants.Story, storyAttributeId }
        };

        var testCase = new AllureTestCase
        {
            Id = TestCaseId,
            Name = "Test Case",
            Status = new Status { Name = "Active" }
        };

        var customFields = new List<AllureCustomField>
        {
            new()
            {
                CustomField = new CustomField { Name = Constants.Feature },
                Name = "Feature1"
            },
            new()
            {
                CustomField = new CustomField { Name = Constants.Story },
                Name = "Story1"
            }
        };

        _client.Setup(x => x.GetTestCaseIdsFromMainSuite(ProjectId))
            .ReturnsAsync(new List<long> { TestCaseId });
        _client.Setup(x => x.GetTestCaseById(TestCaseId))
            .ReturnsAsync(testCase);
        _client.Setup(x => x.GetCustomFieldsFromTestCase(TestCaseId))
            .ReturnsAsync(customFields);

        // Act
        var result = await _sut.ConvertTestCases(ProjectId, new Dictionary<string, Guid>(), attributes, sectionInfo);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1), "Should have exactly one test case");
            Assert.That(sectionInfo.MainSection.Sections, Has.Count.EqualTo(1), "Should have one feature section");

            var featureSection = sectionInfo.MainSection.Sections[0];
            Assert.That(featureSection.Name, Is.EqualTo("Feature1"), "Feature section should have correct name");
            Assert.That(featureSection.Sections, Is.Not.Null, "Feature section should have Sections collection initialized");
            Assert.That(featureSection.Sections, Has.Count.EqualTo(1), "Feature section should have one story section");

            var storySection = featureSection.Sections[0];
            Assert.That(storySection.Name, Is.EqualTo("Story1"), "Story section should have correct name");
            Assert.That(result[0].SectionId, Is.EqualTo(storySection.Id), "Test case should be in story section");
            Assert.That(result[0].Name, Is.EqualTo("Test Case"), "Test case should have correct name");
        });

        sectionInfo.MainSection.Sections ??= new List<Section>();
    }
}
