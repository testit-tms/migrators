using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TestCollabExporter.Client;
using TestCollabExporter.Models;
using TestCollabExporter.Services;

namespace TestCollabExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;
    private Dictionary<int, Guid> _sectionMap;
    private Dictionary<string, Guid> _attributes;
    private Dictionary<int, Guid> _sharedStepsMap;
    private List<TestCollabTestCase> _testCollabTestCases;

    private const int ProjectId = 1;


    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _attachmentService = Substitute.For<IAttachmentService>();

        _sectionMap = new Dictionary<int, Guid>
        {
            { 1, Guid.NewGuid() }
        };

        _attributes = new Dictionary<string, Guid>
        {
            { "Attribute1", Guid.NewGuid() },
            { "Attribute2", Guid.NewGuid() }
        };

        _sharedStepsMap = new Dictionary<int, Guid>
        {
            { 1, Guid.NewGuid() },
            { 2, Guid.NewGuid() },
            { 3, Guid.NewGuid() }
        };

        _testCollabTestCases = new List<TestCollabTestCase>
        {
            new()
            {
                Title = "Test Case 1",
                Description =
                    """Test Case 1 Description <div class="attachment"><div data-attr="{&quot;link&quot;:&quot;https://tcv2-prod.s3.amazonaws.com/6ec42580176a49e0a43770b628370cb1.jpg&quot;,&quot;name&quot;:&quot;7.jpg&quot;,&quot;ext&quot;:&quot;.jpg&quot;}"><img src="https://tcv2-prod.s3.amazonaws.com/6ec42580176a49e0a43770b628370cb1.jpg" class=" at-image"></div></div>""",
                Steps = new List<Steps>
                {
                    new()
                    {
                        Step = "Test Case 1 Step 1",
                        ExpectedResult = "Test Case 1 Expected Result 1"
                    },
                    new()
                    {
                        Step = "",
                        ExpectedResult = "",
                        ReusableStepId = 1
                    }
                },
                Priority = "1",
                CustomFields = new List<CustomField>
                {
                    new()
                    {
                        Name = "Attribute1",
                        Value = "Attribute1 Value"
                    }
                },
                ExecutionTime = 1000,
                Attachments = new List<Attachments>
                {
                    new()
                    {
                        Name = "Test Case 1 Attachment 1",
                        Url = "https://testcollab.com/attachment1"
                    }
                },
                Tags = new List<Tags>
                {
                    new()
                    {
                        Name = "Tag1"
                    },
                    new()
                    {
                        Name = "Tag2"
                    }
                }
            },
            new()
            {
                Title = "Test Case 2",
                Description =
                    "Test Case 2 Description ",
                Steps = new List<Steps>
                {
                    new()
                    {
                        Step = "Test Case 2 Step 1",
                        ExpectedResult = "Test Case 2 Expected Result 1"
                    }
                },
                Priority = "4",
                CustomFields = null,
                ExecutionTime = 1000,
                Attachments = new List<Attachments>
                {
                    new()
                    {
                        Name = "Test Case 2 Attachment 1",
                        Url = "https://testcollab.com/attachment1"
                    }
                },
                Tags = new List<Tags>
                {
                    new()
                    {
                        Name = "Tag1"
                    },
                    new()
                    {
                        Name = "Tag2"
                    }
                }
            }
        };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCases()
    {
        // Arrange
        _client.GetTestCases(ProjectId, _sectionMap.Keys.First())
            .Throws(new Exception("Failed to get test cases"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _attributes, _sharedStepsMap));

        // Assert
        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertTestCases_FailedDownloadAttachment()
    {
        // Arrange
        _client.GetTestCases(ProjectId, _sectionMap.Keys.First())
            .Returns(_testCollabTestCases);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(), _testCollabTestCases[0].Attachments[0].Url,
                _testCollabTestCases[0].Attachments[0].Name)
            .Throws(new Exception("Failed to download attachment"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _attributes, _sharedStepsMap));
    }

    [Test]
    public async Task ConvertTestCases_Success()
    {
        // Arrange
        _client.GetTestCases(ProjectId, _sectionMap.Keys.First())
            .Returns(_testCollabTestCases);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(), _testCollabTestCases[0].Attachments[0].Url,
                _testCollabTestCases[0].Attachments[0].Name)
            .Returns(_testCollabTestCases[0].Attachments[0].Name);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(), _testCollabTestCases[1].Attachments[0].Url,
                _testCollabTestCases[1].Attachments[0].Name)
            .Returns(_testCollabTestCases[1].Attachments[0].Name);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(),
                "https://tcv2-prod.s3.amazonaws.com/6ec42580176a49e0a43770b628370cb1.jpg",
                "6ec42580176a49e0a43770b628370cb1.jpg")
            .Returns("6ec42580176a49e0a43770b628370cb1.jpg");

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        var testCases =
            await testCaseService.ConvertTestCases(ProjectId, _sectionMap, _attributes, _sharedStepsMap);

        // Assert
        Assert.That(testCases, Has.Count.EqualTo(2));
        Assert.That(testCases[0].Name, Is.EqualTo(_testCollabTestCases[0].Title));
        Assert.That(testCases[0].Description, Is.EqualTo("Test Case 1 Description "));
        Assert.That(testCases[0].SectionId, Is.EqualTo(_sectionMap[_sectionMap.Keys.First()]));
        Assert.That(testCases[0].State, Is.EqualTo(StateType.NotReady));
        Assert.That(testCases[0].Priority, Is.EqualTo(PriorityType.Medium));
        Assert.That(testCases[0].PreconditionSteps, Has.Count.EqualTo(0));
        Assert.That(testCases[0].PostconditionSteps, Has.Count.EqualTo(0));
        Assert.That(testCases[0].Attributes, Has.Count.EqualTo(2));
        Assert.That(testCases[0].Attributes[0].Id, Is.EqualTo(_attributes["Attribute1"]));
        Assert.That(testCases[0].Attributes[0].Value, Is.EqualTo("Attribute1 Value"));
        Assert.That(testCases[0].Attributes[1].Id, Is.EqualTo(_attributes["Attribute2"]));
        Assert.That(testCases[0].Attributes[1].Value, Is.Empty);
        Assert.That(testCases[0].Steps, Has.Count.EqualTo(2));
        Assert.That(testCases[0].Steps[0].Action, Is.EqualTo("Test Case 1 Step 1"));
        Assert.That(testCases[0].Steps[0].Expected, Is.EqualTo("Test Case 1 Expected Result 1"));
        Assert.That(testCases[0].Steps[0].SharedStepId, Is.Null);
        Assert.That(testCases[0].Steps[0].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[0].Steps[0].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[0].Steps[0].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[0].Steps[1].Action, Is.Empty);
        Assert.That(testCases[0].Steps[1].Expected, Is.Empty);
        Assert.That(testCases[0].Steps[1].SharedStepId, Is.EqualTo(_sharedStepsMap[1]));
        Assert.That(testCases[0].Steps[1].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[0].Steps[1].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[0].Steps[1].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[0].Duration, Is.EqualTo(1000));
        Assert.That(testCases[0].Tags, Has.Count.EqualTo(2));
        Assert.That(testCases[0].Tags[0], Is.EqualTo("Tag1"));
        Assert.That(testCases[0].Tags[1], Is.EqualTo("Tag2"));
        Assert.That(testCases[0].Attachments, Has.Count.EqualTo(2));
        Assert.That(testCases[0].Attachments[0], Is.EqualTo("Test Case 1 Attachment 1"));
        Assert.That(testCases[0].Attachments[1], Is.EqualTo("6ec42580176a49e0a43770b628370cb1.jpg"));
        Assert.That(testCases[0].Iterations, Has.Count.EqualTo(0));
        Assert.That(testCases[0].Links, Has.Count.EqualTo(0));

        Assert.That(testCases[1].Name, Is.EqualTo(_testCollabTestCases[1].Title));
        Assert.That(testCases[1].Description, Is.EqualTo("Test Case 2 Description "));
        Assert.That(testCases[1].SectionId, Is.EqualTo(_sectionMap[_sectionMap.Keys.First()]));
        Assert.That(testCases[1].State, Is.EqualTo(StateType.NotReady));
        Assert.That(testCases[1].Priority, Is.EqualTo(PriorityType.Medium));
        Assert.That(testCases[1].PreconditionSteps, Has.Count.EqualTo(0));
        Assert.That(testCases[1].PostconditionSteps, Has.Count.EqualTo(0));
        Assert.That(testCases[1].Attributes, Has.Count.EqualTo(2));
        Assert.That(testCases[1].Attributes[0].Id, Is.EqualTo(_attributes["Attribute1"]));
        Assert.That(testCases[1].Attributes[0].Value, Is.Empty);
        Assert.That(testCases[1].Attributes[1].Id, Is.EqualTo(_attributes["Attribute2"]));
        Assert.That(testCases[1].Attributes[1].Value, Is.Empty);
        Assert.That(testCases[1].Steps, Has.Count.EqualTo(1));
        Assert.That(testCases[1].Steps[0].Action, Is.EqualTo("Test Case 2 Step 1"));
        Assert.That(testCases[1].Steps[0].Expected, Is.EqualTo("Test Case 2 Expected Result 1"));
        Assert.That(testCases[1].Steps[0].SharedStepId, Is.Null);
        Assert.That(testCases[1].Steps[0].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[1].Steps[0].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[1].Steps[0].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(testCases[1].Duration, Is.EqualTo(1000));
        Assert.That(testCases[1].Tags, Has.Count.EqualTo(2));
        Assert.That(testCases[1].Tags[0], Is.EqualTo("Tag1"));
        Assert.That(testCases[1].Tags[1], Is.EqualTo("Tag2"));
        Assert.That(testCases[1].Attachments, Has.Count.EqualTo(1));
        Assert.That(testCases[1].Attachments[0], Is.EqualTo("Test Case 2 Attachment 1"));
        Assert.That(testCases[1].Iterations, Has.Count.EqualTo(0));
        Assert.That(testCases[1].Links, Has.Count.EqualTo(0));
        }
}
