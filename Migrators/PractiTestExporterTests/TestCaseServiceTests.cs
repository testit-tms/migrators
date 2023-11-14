using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PractiTestExporter.Client;
using PractiTestExporter.Models;
using PractiTestExporter.Services;

namespace PractiTestExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;
    private Dictionary<string, Guid> _attributeMap;
    private List<PractiTestTestCase> _tests;
    private List<PractiTestStep> _steps;
    private List<PractiTestTestCase> _sharedSteps;
    private List<PractiTestStep> _sharedStepSteps;
    private List<string> _attachments;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _attachmentService = Substitute.For<IAttachmentService>();

        _attributeMap = new Dictionary<string, Guid>
        {
            { "222", new Guid() },
            { "111", new Guid() }
        };

        _sharedSteps = new List<PractiTestTestCase>
        {
            new()
            {
                Id = "777",
                Attributes = new TestCaseAttributes
                {
                    Name = "shared test 1",
                    Preconditions = "precon 1",
                    Description = "desc 1",
                    Tags = new List<string> { "tag1", "tag2", "tag3" },
                    CustomFields = new Dictionary<string, string>
                    {
                        { "---f-222", "value1" },
                        { "---f-111", "value2" }
                    }
                }
            },
            new()
            {
                Id = "888",
                Attributes = new TestCaseAttributes
                {
                    Name = "shared test 2",
                    Preconditions = "precon 2",
                    Description = "desc 2",
                    Tags = new List<string> { "tag11", "tag21", "tag31" },
                    CustomFields = new Dictionary<string, string>
                    {
                        { "---f-222", "value1" },
                        { "---f-111", "value2" }
                    }
                }
            },
        };

        _tests = new List<PractiTestTestCase>
        {
            _sharedSteps[0],
            new()
            {
                Id = "123",
                Attributes = new TestCaseAttributes
                {
                    Name = "test2",
                    Preconditions = "precon2",
                    Description = "desc2",
                    Tags = new List<string> { "tag3", "tag2", "tag1" },
                    CustomFields = new Dictionary<string, string>
                    {
                        { "---f-222", "value1" },
                        { "---f-111", "value2" }
                    }
                }
            },
            _sharedSteps[1],
        };

        _steps = new List<PractiTestStep>
        {
            new()
            {
                Id = "1",
                Attributes = new StepAttributes
                {
                    Name = "step1",
                    Description = "desc",
                    ExpectedResults = "expected",
                    TestToCallId = null
                }
            },
            new()
            {
                Id = "1",
                Attributes = new StepAttributes
                {
                    TestToCallId = 777
                }
            },
            new()
            {
                Id = "1",
                Attributes = new StepAttributes
                {
                    TestToCallId = 888
                }
            },
            new()
            {
                Id = "1",
                Attributes = new StepAttributes
                {
                    TestToCallId = 777
                }
            },
        };

        _sharedStepSteps = new List<PractiTestStep>
        {
            new()
            {
                Id = "2",
                Attributes = new StepAttributes
                {
                    Name = "step1",
                    Description = "desc1",
                    ExpectedResults = "expected1",
                    TestToCallId = null
                }
            },
                        new()
            {
                Id = "2",
                Attributes = new StepAttributes
                {
                    Name = "step2",
                    Description = "desc2",
                    ExpectedResults = "expected2",
                    TestToCallId = null
                }
            }
        };

        _attachments = new List<string>() { "Test.txt" };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCases()
    {
        // Arrange
        var sectionId = new Guid();

        _client.GetTestCases()
            .Throws(new Exception("Failed to get test"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() => testCaseService.ConvertTestCases(sectionId, _attributeMap));

        // Assert
        await _client.DidNotReceive()
            .GetTestCaseById(Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task ConvertTestCases_FailedStepsByTestCaseId()
    {
        // Arrange
        var sectionId = new Guid();

        _client.GetTestCases()
            .Returns(_tests);

        _attachmentService.DownloadAttachments(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>())
            .Returns(_attachments);

        _client.GetStepsByTestCaseId(Arg.Any<string>())
            .Throws(new Exception("Failed to get steps"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() => testCaseService.ConvertTestCases(sectionId, _attributeMap));
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestCaseById()
    {
        // Arrange
        var sectionId = new Guid();

        _client.GetTestCases()
            .Returns(_tests);

        _client.GetStepsByTestCaseId(_tests[0].Id)
            .Returns(_steps);

        _attachmentService.DownloadAttachments(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>())
            .Returns(_attachments);

        _client.GetTestCaseById(Arg.Any<string>())
            .Throws(new Exception("Failed to get test case"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() => testCaseService.ConvertTestCases(sectionId, _attributeMap));
    }

    [Test]
    public async Task ConvertTestCases_FailedDownloadAttachment()
    {
        // Arrange
        var sectionId = new Guid();

        _client.GetTestCases()
            .Returns(_tests);

        _client.GetStepsByTestCaseId(_tests[0].Id)
            .Returns(_steps);

        _client.GetTestCaseById(Arg.Any<string>())
            .Returns(_sharedSteps[0]);

        _client.GetStepsByTestCaseId(_sharedSteps[0].Id)
            .Returns(_sharedStepSteps);

        _attachmentService.DownloadAttachments(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>())
            .Throws(new Exception("Failed to download attachment"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() => testCaseService.ConvertTestCases(sectionId, _attributeMap));
    }

    [Test]
    public async Task ConvertTestCases_Success_WithSharedStep()
    {
        // Arrange
        var sectionId = new Guid();

        _client.GetTestCases()
            .Returns(_tests);

        _client.GetStepsByTestCaseId(_tests[1].Id)
            .Returns(_steps);

        _client.GetTestCaseById(_sharedSteps[0].Id)
            .Returns(_sharedSteps[0]);

        _client.GetTestCaseById(_sharedSteps[1].Id)
            .Returns(_sharedSteps[1]);

        _client.GetStepsByTestCaseId(_sharedSteps[0].Id)
            .Returns(_sharedStepSteps);

        _client.GetStepsByTestCaseId(_sharedSteps[1].Id)
            .Returns(_sharedStepSteps);

        _attachmentService.DownloadAttachments(Arg.Any<string>(), _tests[1].Id, Arg.Any<Guid>())
            .Returns(_attachments);

        _attachmentService.DownloadAttachments(Arg.Any<string>(), _steps[0].Id, Arg.Any<Guid>())
            .Returns(new List<string>());

        _attachmentService.DownloadAttachments(Arg.Any<string>(), _sharedSteps[0].Id, Arg.Any<Guid>())
            .Returns(new List<string>());

        _attachmentService.DownloadAttachments(Arg.Any<string>(), _sharedSteps[1].Id, Arg.Any<Guid>())
    .Returns(new List<string>());

        _attachmentService.DownloadAttachments(Arg.Any<string>(), _sharedStepSteps[0].Id, Arg.Any<Guid>())
            .Returns(_attachments);

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        var result = await testCaseService.ConvertTestCases(sectionId, _attributeMap);

        // Assert
        Assert.That(result.TestCases, Has.Count.EqualTo(1));
        Assert.That(result.TestCases[0].Name, Is.EqualTo(_tests[1].Attributes.Name));
        Assert.That(result.TestCases[0].Description, Is.EqualTo(_tests[1].Attributes.Description));
        Assert.That(result.TestCases[0].PreconditionSteps, Has.Count.EqualTo(1));
        Assert.That(result.TestCases[0].PreconditionSteps[0].Action, Is.EqualTo(_tests[1].Attributes.Preconditions));
        Assert.That(result.TestCases[0].Steps, Has.Count.EqualTo(4));
        Assert.That(result.TestCases[0].Steps[0].Action, Is.EqualTo(_steps[0].Attributes.Name));
        Assert.That(result.TestCases[0].Steps[0].Expected, Is.EqualTo(_steps[0].Attributes.ExpectedResults));
        Assert.That(result.TestCases[0].Steps[0].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[0].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[0].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[0].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[1].Action, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[1].Expected, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[1].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[1].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[1].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[1].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[2].Action, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[2].Expected, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[2].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[2].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[2].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[2].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[3].Action, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[3].Expected, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[3].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[3].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[3].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[3].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Tags, Has.Count.EqualTo(3));
        Assert.That(result.TestCases[0].Attributes, Has.Count.EqualTo(2));
        Assert.That(result.TestCases[0].Attributes[0].Id, Is.EqualTo(_attributeMap["222"]));
        Assert.That(result.TestCases[0].Attributes[0].Value, Is.EqualTo(_tests[0].Attributes.CustomFields["---f-222"]));
        Assert.That(result.TestCases[0].Attributes[1].Id, Is.EqualTo(_attributeMap["111"]));
        Assert.That(result.TestCases[0].Attributes[1].Value, Is.EqualTo(_tests[0].Attributes.CustomFields["---f-111"]));
        Assert.That(result.TestCases[0].Tags[0], Is.EqualTo("tag3"));
        Assert.That(result.TestCases[0].Tags[1], Is.EqualTo("tag2"));
        Assert.That(result.TestCases[0].Tags[2], Is.EqualTo("tag1"));
        Assert.That(result.TestCases[0].Links, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Attachments, Has.Count.EqualTo(1));
        Assert.That(result.TestCases[0].Attachments[0], Is.EqualTo("Test.txt"));
        Assert.That(result.SharedSteps, Has.Count.EqualTo(2));
        Assert.That(result.SharedSteps[0].Name, Is.EqualTo(_sharedSteps[0].Attributes.Name));
        Assert.That(result.SharedSteps[0].Description, Is.EqualTo(_sharedSteps[0].Attributes.Description));
        Assert.That(result.SharedSteps[0].Steps, Has.Count.EqualTo(2));
        Assert.That(result.SharedSteps[0].Steps[0].Action, Is.EqualTo(_sharedStepSteps[0].Attributes.Name + $"<br><p><<<{_attachments[0]}>>></p>"));
        Assert.That(result.SharedSteps[0].Steps[0].Expected, Is.EqualTo(_sharedStepSteps[0].Attributes.ExpectedResults));
        Assert.That(result.SharedSteps[0].Steps[0].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.SharedSteps[0].Steps[0].ActionAttachments, Has.Count.EqualTo(1));
        Assert.That(result.SharedSteps[0].Steps[0].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[0].Steps[0].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[0].Steps[1].Action, Is.EqualTo(_sharedStepSteps[1].Attributes.Name + $"<br><p><<<{_attachments[0]}>>></p>"));
        Assert.That(result.SharedSteps[0].Steps[1].Expected, Is.EqualTo(_sharedStepSteps[1].Attributes.ExpectedResults));
        Assert.That(result.SharedSteps[0].Steps[1].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.SharedSteps[0].Steps[1].ActionAttachments, Has.Count.EqualTo(1));
        Assert.That(result.SharedSteps[0].Steps[1].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[0].Steps[1].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[0].Tags, Has.Count.EqualTo(3));
        Assert.That(result.SharedSteps[0].Tags[0], Is.EqualTo("tag1"));
        Assert.That(result.SharedSteps[0].Tags[1], Is.EqualTo("tag2"));
        Assert.That(result.SharedSteps[0].Tags[2], Is.EqualTo("tag3"));
        Assert.That(result.SharedSteps[0].Links, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[0].Attachments, Has.Count.EqualTo(2));
        Assert.That(result.SharedSteps[1].Name, Is.EqualTo(_sharedSteps[1].Attributes.Name));
        Assert.That(result.SharedSteps[1].Description, Is.EqualTo(_sharedSteps[1].Attributes.Description));
        Assert.That(result.SharedSteps[1].Steps, Has.Count.EqualTo(2));
        Assert.That(result.SharedSteps[1].Steps[0].Action, Is.EqualTo(_sharedStepSteps[0].Attributes.Name + $"<br><p><<<{_attachments[0]}>>></p>"));
        Assert.That(result.SharedSteps[1].Steps[0].Expected, Is.EqualTo(_sharedStepSteps[0].Attributes.ExpectedResults));
        Assert.That(result.SharedSteps[1].Steps[0].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.SharedSteps[1].Steps[0].ActionAttachments, Has.Count.EqualTo(1));
        Assert.That(result.SharedSteps[1].Steps[0].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[1].Steps[0].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[1].Steps[1].Action, Is.EqualTo(_sharedStepSteps[1].Attributes.Name + $"<br><p><<<{_attachments[0]}>>></p>"));
        Assert.That(result.SharedSteps[1].Steps[1].Expected, Is.EqualTo(_sharedStepSteps[1].Attributes.ExpectedResults));
        Assert.That(result.SharedSteps[1].Steps[1].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.SharedSteps[1].Steps[1].ActionAttachments, Has.Count.EqualTo(1));
        Assert.That(result.SharedSteps[1].Steps[1].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[1].Steps[1].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[1].Tags, Has.Count.EqualTo(3));
        Assert.That(result.SharedSteps[1].Tags[0], Is.EqualTo("tag11"));
        Assert.That(result.SharedSteps[1].Tags[1], Is.EqualTo("tag21"));
        Assert.That(result.SharedSteps[1].Tags[2], Is.EqualTo("tag31"));
        Assert.That(result.SharedSteps[1].Links, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[1].Attachments, Has.Count.EqualTo(2));
    }
}
