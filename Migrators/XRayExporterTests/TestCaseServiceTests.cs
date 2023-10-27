using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using XRayExporter.Client;
using XRayExporter.Models;
using XRayExporter.Services;

namespace XRayExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;
    private Dictionary<int, Guid> _sectionMap;
    private List<XRayTest> _tests;
    private XRayTestFull _testCase;
    private JiraItem _jiraItem;
    private XRayTestFull _shareStep;
    private JiraItem _jiraItem2;

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

        _tests = new List<XRayTest>
        {
            new()
            {
                Key = "TEST-1",
                Id = 1
            }
        };

        _testCase = new XRayTestFull
        {
            Id = _tests[0].Id,
            Key = _tests[0].Key,
            Self = "https://xray.cloud.xpand-it.com/api/v2/tests/1",
            Reporter = "admin",
            Preconditions = new List<Precondition>
            {
                new()
                {
                    Condition = "Precondition 1"
                }
            },
            Type = "Manual",
            Status = "Approved",
            Archived = false,
            Definition = new Definition
            {
                Steps = new List<Steps>
                {
                    new()
                    {
                        Step = new Step
                        {
                            Rendered = "Step 1",
                        },
                        Data = new Data
                        {
                            Rendered = "Data 1"
                        },
                        Result = new Result
                        {
                            Rendered = "Result 1"
                        },
                        Attachments = new List<XRayAttachments>
                        {
                            new()
                            {
                                FileURL = "https://xray.cloud.xpand-it.com/secure/attachment/1/2.png",
                                FileName = "2.png"
                            }
                        }
                    },
                    new()
                    {
                        Step = new Step
                        {
                            Rendered =
                                """<p><font color="#C1C7D0"> This step was calling test issue <a href="https://jira.testit.ru/browse/TEST-2" title="Test 02" class="issue-link" data-issue-key="XRAYT-2">XRAYT-2</a> (possibly downgraded)</font></p>""",
                        },
                        Data = new Data
                        {
                            Rendered = string.Empty
                        },
                        Result = new Result
                        {
                            Rendered = string.Empty
                        },
                        Attachments = new List<XRayAttachments>()
                    }
                }
            }
        };

        _jiraItem = new JiraItem
        {
            Fields = new Fields
            {
                Description = "Description",
                Attachments = new List<Attachment>
                {
                    new()
                    {
                        Content = "https://xray.cloud.xpand-it.com/secure/attachment/1/1.png",
                        FileName = "1.png"
                    }
                },
                Summary = "Summary",
                Labels = new List<string>
                {
                    "Label 1",
                    "Label 2"
                },
                IssueLinks = new List<JiraLink>
                {
                    new()
                    {
                        Type = new XRayExporter.Models.Type
                        {
                            Name = "Relates",
                            Inward = "is tested by"
                        },
                        InwardIssue = new Issue
                        {
                            Key = "TEST-2",
                            Self = "https://xray.cloud.xpand-it.com/rest/api/2/issue/43321"
                        },
                        OutwardIssue = null
                    },
                    new()
                    {
                        Type = new XRayExporter.Models.Type
                        {
                            Name = "Problem/Incident",
                            Inward = "is caused by"
                        },
                        InwardIssue = null,
                        OutwardIssue = new Issue
                        {
                            Key = "TEST-3",
                            Self = "https://xray.cloud.xpand-it.com/rest/api/3/issue/43321"
                        }
                    }
                }
            }
        };

        _shareStep = new XRayTestFull
        {
            Id = 2,
            Key = "TEST-2",
            Self = "https://xray.cloud.xpand-it.com/api/v2/tests/2",
            Reporter = "admin",
            Preconditions = new List<Precondition>
            {
                new()
                {
                    Condition = "Precondition 1"
                }
            },
            Type = "Automated",
            Status = "Draft",
            Archived = false,
            Definition = new Definition
            {
                Steps = new List<Steps>()
            }
        };

        _jiraItem2 = new JiraItem
        {
            Fields = new Fields
            {
                Description = "Description",
                Attachments = new List<Attachment>(),
                Summary = "Summary",
                Labels = new List<string>
                {
                    "Label 1",
                    "Label 2"
                },
                IssueLinks = new List<JiraLink>()
            }
        };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTestFromFolder()
    {
        // Arrange
        _client.GetTestFromFolder(_sectionMap.First().Key)
            .Throws(new Exception("Failed to get test from folder"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() => testCaseService.ConvertTestCases(_sectionMap));

        // Assert
        await _client.DidNotReceive()
            .GetTest(Arg.Any<string>());

        await _client.DidNotReceive()
            .GetItem(Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTest()
    {
        // Arrange
        _client.GetTestFromFolder(_sectionMap.First().Key)
            .Returns(_tests);

        _client.GetTest(_tests[0].Key)
            .Throws(new Exception("Failed to get test"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() => testCaseService.ConvertTestCases(_sectionMap));

        // Assert
        await _client.DidNotReceive()
            .GetItem(Arg.Any<string>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetItem()
    {
        // Arrange
        _client.GetTestFromFolder(_sectionMap.First().Key)
            .Returns(_tests);

        _client.GetTest(_tests[0].Key)
            .Returns(_testCase);

        _client.GetItem(_testCase.Self)
            .Throws(new Exception("Failed to get item"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() => testCaseService.ConvertTestCases(_sectionMap));

        // Assert
        await _attachmentService.DidNotReceive()
            .DownloadAttachment(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task ConvertTestCases_FailedDownloadAttachment()
    {
        // Arrange
        _client.GetTestFromFolder(_sectionMap.First().Key)
            .Returns(_tests);

        _client.GetTest(_tests[0].Key)
            .Returns(_testCase);

        _client.GetItem(_testCase.Self)
            .Returns(_jiraItem);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(), _testCase.Definition.Steps[0].Attachments[0].FileURL,
                _testCase.Definition.Steps[0].Attachments[0].FileName)
            .Throws(new Exception("Failed to download attachment"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() => testCaseService.ConvertTestCases(_sectionMap));
    }

    [Test]
    public async Task ConvertTestCases_Success_WithSharedStep()
    {
        // Arrange
        _client.GetTestFromFolder(_sectionMap.First().Key)
            .Returns(_tests);

        _client.GetTest(_tests[0].Key)
            .Returns(_testCase);

        _client.GetTest(_shareStep.Key)
            .Returns(_shareStep);

        _client.GetItem(_testCase.Self)
            .Returns(_jiraItem);

        _client.GetItem(_shareStep.Self)
            .Returns(_jiraItem2);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(), _testCase.Definition.Steps[0].Attachments[0].FileURL,
                _testCase.Definition.Steps[0].Attachments[0].FileName)
            .Returns(_testCase.Definition.Steps[0].Attachments[0].FileName);

        _attachmentService.DownloadAttachment(Arg.Any<Guid>(), _jiraItem.Fields.Attachments[0].Content,
                _jiraItem.Fields.Attachments[0].FileName)
            .Returns(_jiraItem.Fields.Attachments[0].FileName);

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        var result = await testCaseService.ConvertTestCases(_sectionMap);

        // Assert
        Assert.That(result.Attributes, Has.Count.EqualTo(4));
        Assert.That(result.Attributes[0].Name, Is.EqualTo(Constants.XrayReporter));
        Assert.That(result.Attributes[0].Options, Has.Count.EqualTo(0));
        Assert.That(result.Attributes[1].Name, Is.EqualTo(Constants.XrayType));
        Assert.That(result.Attributes[1].Options, Has.Count.EqualTo(2));
        Assert.That(result.Attributes[1].Options[1], Is.EqualTo("Manual"));
        Assert.That(result.Attributes[1].Options[0], Is.EqualTo("Automated"));
        Assert.That(result.Attributes[2].Name, Is.EqualTo(Constants.XrayStatus));
        Assert.That(result.Attributes[2].Options, Has.Count.EqualTo(2));
        Assert.That(result.Attributes[2].Options[1], Is.EqualTo("Approved"));
        Assert.That(result.Attributes[2].Options[0], Is.EqualTo("Draft"));
        Assert.That(result.Attributes[3].Name, Is.EqualTo(Constants.XrayArchived));
        Assert.That(result.Attributes[3].Options, Has.Count.EqualTo(1));
        Assert.That(result.Attributes[3].Options[0], Is.EqualTo("False"));
        Assert.That(result.TestCases, Has.Count.EqualTo(1));
        Assert.That(result.TestCases[0].Name, Is.EqualTo(_jiraItem.Fields.Summary));
        Assert.That(result.TestCases[0].Description, Is.EqualTo(_jiraItem.Fields.Description));
        Assert.That(result.TestCases[0].PreconditionSteps, Has.Count.EqualTo(1));
        Assert.That(result.TestCases[0].PreconditionSteps[0].Action, Is.EqualTo(_testCase.Preconditions[0].Condition));
        Assert.That(result.TestCases[0].Steps, Has.Count.EqualTo(2));
        Assert.That(result.TestCases[0].Steps[0].Action, Is.EqualTo("Step 1<br><p><<<2.png>>></p>"));
        Assert.That(result.TestCases[0].Steps[0].Expected, Is.EqualTo(_testCase.Definition.Steps[0].Result.Rendered));
        Assert.That(result.TestCases[0].Steps[0].TestData, Is.EqualTo(_testCase.Definition.Steps[0].Data.Rendered));
        Assert.That(result.TestCases[0].Steps[0].ActionAttachments, Has.Count.EqualTo(1));
        Assert.That(result.TestCases[0].Steps[0].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[0].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[1].Action, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[1].Expected, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[1].TestData, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases[0].Steps[1].ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[1].ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Steps[1].TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases[0].Tags, Has.Count.EqualTo(2));
        Assert.That(result.TestCases[0].Tags[0], Is.EqualTo("Label 1"));
        Assert.That(result.TestCases[0].Tags[1], Is.EqualTo("Label 2"));
        Assert.That(result.TestCases[0].Links, Has.Count.EqualTo(2));
        Assert.That(result.TestCases[0].Links[0].Description, Is.EqualTo("is tested by"));
        Assert.That(result.TestCases[0].Links[0].Url, Is.EqualTo("https://xray.cloud.xpand-it.com/browse/TEST-2"));
        Assert.That(result.TestCases[0].Links[0].Title, Is.EqualTo("Relates"));
        Assert.That(result.TestCases[0].Links[1].Description, Is.EqualTo("is caused by"));
        Assert.That(result.TestCases[0].Links[1].Url, Is.EqualTo("https://xray.cloud.xpand-it.com/browse/TEST-3"));
        Assert.That(result.TestCases[0].Links[1].Title, Is.EqualTo("Problem/Incident"));
        Assert.That(result.TestCases[0].Attachments, Has.Count.EqualTo(2));
        Assert.That(result.TestCases[0].Attachments[0], Is.EqualTo("1.png"));
        Assert.That(result.TestCases[0].Attachments[1], Is.EqualTo("2.png"));
        Assert.That(result.SharedSteps, Has.Count.EqualTo(1));
        Assert.That(result.SharedSteps[0].Name, Is.EqualTo(_jiraItem2.Fields.Summary));
        Assert.That(result.SharedSteps[0].Description, Is.EqualTo(_jiraItem2.Fields.Description));
        Assert.That(result.SharedSteps[0].Steps, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[0].Tags, Has.Count.EqualTo(2));
        Assert.That(result.SharedSteps[0].Tags[0], Is.EqualTo("Label 1"));
        Assert.That(result.SharedSteps[0].Tags[1], Is.EqualTo("Label 2"));
        Assert.That(result.SharedSteps[0].Links, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps[0].Attachments, Has.Count.EqualTo(0));
    }
}
