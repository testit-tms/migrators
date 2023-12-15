using HPALMExporter.Client;
using HPALMExporter.Models;
using HPALMExporter.Services;
using ImportHPALMToTestIT.Models.HPALM;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HPALMExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IAttachmentService _attachmentService;

    private Dictionary<int, Guid> _sectionMap;
    private Dictionary<string, Guid> _attributeMap;
    private List<HPALMTest> _tests;
    private List<HPALMParameter> _testParamaters;
    private AttachmentData _testAttachmentData;
    private List<HPALMStep> _testSteps;
    private AttachmentData _testStepAttachmentData;

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

        _attributeMap = new Dictionary<string, Guid>
        {
            { "Attribute", Guid.NewGuid() }
        };

        _tests = new List<HPALMTest>
        {
            new()
            {
                Id = 1,
                Name = "Test",
                Description = "Description",
                HasAttachments = true,
                ParentId = 1,
                Attrubites = new Dictionary<string, string>
                {
                    { "Attribute", "Value" }
                }
            },
            new()
            {
                Id = 2,
                Name = "Shared step",
                Description = "Description",
                HasAttachments = true,
                ParentId = 1,
                Attrubites = new Dictionary<string, string>
                {
                    { "Attribute", "Value" }
                }
            }
        };

        _testParamaters = new List<HPALMParameter>
        {
            new()
            {
                Name = "Parameter",
                Value = "Value"
            }
        };

        _testAttachmentData = new AttachmentData
        {
            Attachments = new List<string>
            {
                "Attachment"
            },
            Links = new List<Link>
            {
                new()
                {
                    Url = "Url",
                    Title = "Title"
                }
            }
        };

        _testSteps = new List<HPALMStep>
        {
            new()
            {
                Id = 12,
                Name = "Step",
                Description = "Description",
                HasAttachments = true,
                ParentId = 1,
                Expected = "Expected",
            },
            new()
            {
                Id = 13,
                Name = "Step",
                Description = "Description",
                HasAttachments = true,
                ParentId = 1,
                Expected = "Expected",
                LinkId = 2
            }
        };

        _testStepAttachmentData = new AttachmentData
        {
            Attachments = new List<string>
            {
                "Attachment1"
            },
            Links = new List<Link>
            {
                new()
                {
                    Url = "Url1",
                    Title = "Title1"
                }
            }
        };
    }

    [Test]
    public async Task ConvertTestCases_FailedGetTests()
    {
        // Arrange
        _client.GetTests(_sectionMap.Keys.First(),
                Arg.Is<List<string>>(a => a.SequenceEqual(_attributeMap.Keys.ToList())))
            .Throws(new Exception("Failed to get tests"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap, _attributeMap));

        // Assert
        await _client.DidNotReceive()
            .GetParameters(Arg.Any<int>());

        await _attachmentService.DidNotReceive()
            .ConvertAttachmentsFromTest(Arg.Any<Guid>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetSteps(Arg.Any<int>());

        await _attachmentService.DidNotReceive()
            .ConvertAttachmentsFromStep(Arg.Any<Guid>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetTest(Arg.Any<int>(), Arg.Any<List<string>>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetParameters()
    {
        // Arrange
        _client.GetTests(_sectionMap.Keys.First(),
                Arg.Is<List<string>>(a => a.SequenceEqual(_attributeMap.Keys.ToList())))
            .Returns(_tests);

        _client.GetParameters(_tests.First().Id)
            .Throws(new Exception("Failed to get parameters"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap, _attributeMap));

        // Assert
        await _attachmentService.DidNotReceive()
            .ConvertAttachmentsFromTest(Arg.Any<Guid>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetSteps(Arg.Any<int>());

        await _attachmentService.DidNotReceive()
            .ConvertAttachmentsFromStep(Arg.Any<Guid>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetTest(Arg.Any<int>(), Arg.Any<List<string>>());
    }

    [Test]
    public async Task ConvertTestCases_FailedConvertAttachmentsFromTest()
    {
        // Arrange
        _client.GetTests(_sectionMap.Keys.First(),
                Arg.Is<List<string>>(a => a.SequenceEqual(_attributeMap.Keys.ToList())))
            .Returns(_tests);

        _client.GetParameters(_tests.First().Id)
            .Returns(_testParamaters);

        _attachmentService.ConvertAttachmentsFromTest(Arg.Any<Guid>(), _sectionMap.Keys.First())
            .Throws(new Exception("Failed to convert attachments from test"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap, _attributeMap));

        // Assert
        await _client.DidNotReceive()
            .GetSteps(Arg.Any<int>());

        await _attachmentService.DidNotReceive()
            .ConvertAttachmentsFromStep(Arg.Any<Guid>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetTest(Arg.Any<int>(), Arg.Any<List<string>>());
    }

    [Test]
    public async Task ConvertTestCases_FailedGetSteps()
    {
        // Arrange
        _client.GetTests(_sectionMap.Keys.First(),
                Arg.Is<List<string>>(a => a.SequenceEqual(_attributeMap.Keys.ToList())))
            .Returns(_tests);

        _client.GetParameters(_tests.First().Id)
            .Returns(_testParamaters);

        _attachmentService.ConvertAttachmentsFromTest(Arg.Any<Guid>(), _sectionMap.Keys.First())
            .Returns(_testAttachmentData);

        _client.GetSteps(_tests.First().Id)
            .Throws(new Exception("Failed to get steps"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap, _attributeMap));

        // Assert
        await _attachmentService.DidNotReceive()
            .ConvertAttachmentsFromStep(Arg.Any<Guid>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetTest(Arg.Any<int>(), Arg.Any<List<string>>());
    }

    [Test]
    public async Task ConvertTestCases_FailedConvertAttachmentsFromStep()
    {
        // Arrange
        _client.GetTests(_sectionMap.Keys.First(),
                Arg.Is<List<string>>(a => a.SequenceEqual(_attributeMap.Keys.ToList())))
            .Returns(_tests);

        _client.GetParameters(_tests.First().Id)
            .Returns(_testParamaters);

        _attachmentService.ConvertAttachmentsFromTest(Arg.Any<Guid>(), _sectionMap.Keys.First())
            .Returns(_testAttachmentData);

        _client.GetSteps(_tests.First().Id)
            .Returns(_testSteps);

        _attachmentService.ConvertAttachmentsFromStep(Arg.Any<Guid>(), _testSteps.First().Id)
            .Throws(new Exception("Failed to convert attachments from step"));

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await testCaseService.ConvertTestCases(_sectionMap, _attributeMap));

        // Assert
        await _client.DidNotReceive()
            .GetTest(Arg.Any<int>(), Arg.Any<List<string>>());
    }

    [Test]
    public async Task ConvertTestCases_Success()
    {
        // Arrange
        _client.GetTests(_sectionMap.Keys.First(),
                Arg.Is<List<string>>(a => a.SequenceEqual(_attributeMap.Keys.ToList())))
            .Returns(_tests);

        _client.GetParameters(_tests.First().Id)
            .Returns(_testParamaters);

        _attachmentService.ConvertAttachmentsFromTest(Arg.Any<Guid>(), _tests.First().Id)
            .Returns(_testAttachmentData);

        _client.GetSteps(_tests.First().Id)
            .Returns(_testSteps);

        _attachmentService.ConvertAttachmentsFromStep(Arg.Any<Guid>(), _testSteps.First().Id)
            .Returns(_testStepAttachmentData);

        _client.GetTest(_testSteps.Last().LinkId.Value,
                Arg.Is<List<string>>(a => a.SequenceEqual(_attributeMap.Keys.ToList())))
            .Returns(_tests.Last());

        _client.GetSteps(_tests.Last().Id)
            .Returns(new List<HPALMStep>());

        _attachmentService.ConvertAttachmentsFromTest(Arg.Any<Guid>(), _tests.Last().Id)
            .Returns(new AttachmentData
            {
                Attachments = new List<string>(),
                Links = new List<Link>()
            });

        var testCaseService = new TestCaseService(_logger, _client, _attachmentService);

        // Act
        var result = await testCaseService.ConvertTestCases(_sectionMap, _attributeMap);

        // Assert
        Assert.That(result.TestCases, Has.Count.EqualTo(1));
        Assert.That(result.TestCases.First().Name, Is.EqualTo(_tests.First().Name));
        Assert.That(result.TestCases.First().Description, Is.EqualTo(_tests.First().Description));
        Assert.That(result.TestCases.First().Iterations, Has.Count.EqualTo(1));
        Assert.That(result.TestCases.First().Iterations.First().Parameters, Has.Count.EqualTo(1));
        Assert.That(result.TestCases.First().Iterations.First().Parameters.First().Name,
            Is.EqualTo(_testParamaters.First().Name));
        Assert.That(result.TestCases.First().Iterations.First().Parameters.First().Value,
            Is.EqualTo(_testParamaters.First().Value));
        Assert.That(result.TestCases.First().Steps, Has.Count.EqualTo(2));
        Assert.That(result.TestCases.First().Steps.First().Action, Is.EqualTo(_testSteps.First().Description));
        Assert.That(result.TestCases.First().Steps.First().Expected, Is.EqualTo(_testSteps.First().Expected));
        Assert.That(result.TestCases.First().Steps.First().TestData, Is.EqualTo("<a href=\"Url1\">Title1</a>\n"));
        Assert.That(result.TestCases.First().Steps.First().ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases.First().Steps.First().ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases.First().Steps.First().TestDataAttachments, Has.Count.EqualTo(1));
        Assert.That(result.TestCases.First().Steps.First().TestDataAttachments.First(),
            Is.EqualTo(_testStepAttachmentData.Attachments.First()));
        Assert.That(result.TestCases.First().Steps.Last().Action, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases.First().Steps.Last().Expected, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases.First().Steps.Last().TestData, Is.EqualTo(string.Empty));
        Assert.That(result.TestCases.First().Steps.Last().ActionAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases.First().Steps.Last().ExpectedAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases.First().Steps.Last().TestDataAttachments, Has.Count.EqualTo(0));
        Assert.That(result.TestCases.First().Attachments, Has.Count.EqualTo(2));
        Assert.That(result.TestCases.First().Attachments.First(),
            Is.EqualTo(_testAttachmentData.Attachments.First()));
        Assert.That(result.TestCases.First().Attachments.Last(),
            Is.EqualTo(_testStepAttachmentData.Attachments.First()));
        Assert.That(result.SharedSteps, Has.Count.EqualTo(1));
        Assert.That(result.SharedSteps.First().Name, Is.EqualTo(_tests.Last().Name));
        Assert.That(result.SharedSteps.First().Description, Is.EqualTo(_tests.Last().Description));
        Assert.That(result.SharedSteps.First().Steps, Has.Count.EqualTo(0));
        Assert.That(result.SharedSteps.First().Attachments, Has.Count.EqualTo(0));
    }
}
