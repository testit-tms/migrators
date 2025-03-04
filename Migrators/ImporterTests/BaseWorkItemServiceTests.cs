using System.Text.Json;
using Importer.Models;
using Importer.Services;
using Models;

namespace ImporterTests;

[TestFixture]
public class BaseWorkItemServiceTests
{
    private TestBaseWorkItemService _service = null!;
    private Dictionary<Guid, TmsAttribute> _tmsAttributes = null!;

    private class TestBaseWorkItemService : BaseWorkItemService
    {
        public static List<CaseAttribute> TestConvertAttributes(IEnumerable<CaseAttribute> attributes,
            Dictionary<Guid, TmsAttribute> tmsAttributes)
        {
            return ConvertAttributes(attributes, tmsAttributes);
        }

        public static List<Step> TestAddAttachmentsToSteps(List<Step> steps, Dictionary<string, Guid> attachments)
        {
            return AddAttachmentsToSteps(steps, attachments);
        }
    }

    [SetUp]
    public void Setup()
    {
        _service = new TestBaseWorkItemService();
        _tmsAttributes = new Dictionary<Guid, TmsAttribute>();
    }

    [Test]
    public void ConvertAttributes_WhenOptionsType_ReturnsOptionId()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var optionValue = "Option1";

        _tmsAttributes[attributeId] = new TmsAttribute
        {
            Id = attributeId,
            Type = "options",
            Options = new List<TmsAttributeOptions>
            {
                new() { Id = optionId, Value = optionValue }
            }
        };

        var attributes = new List<CaseAttribute>
        {
            new() { Id = attributeId, Value = optionValue }
        };

        // Act
        var result = TestBaseWorkItemService.TestConvertAttributes(attributes, _tmsAttributes);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(attributeId));
        Assert.That(result[0].Value.ToString(), Is.EqualTo(optionId.ToString()));
    }

    [Test]
    public void ConvertAttributes_WhenMultipleOptionsType_ReturnsOptionIds()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var option1Id = Guid.NewGuid();
        var option2Id = Guid.NewGuid();
        var option1Value = "Option1";
        var option2Value = "Option2";

        _tmsAttributes[attributeId] = new TmsAttribute
        {
            Id = attributeId,
            Type = "multipleOptions",
            Options = new List<TmsAttributeOptions>
            {
                new() { Id = option1Id, Value = option1Value },
                new() { Id = option2Id, Value = option2Value }
            }
        };

        var attributes = new List<CaseAttribute>
        {
            new() { Id = attributeId, Value = JsonSerializer.Serialize(new List<string> { option1Value, option2Value }) }
        };

        // Act
        var result = TestBaseWorkItemService.TestConvertAttributes(attributes, _tmsAttributes);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(attributeId));
        var values = (List<string>)result[0].Value;
        Assert.That(values.Count, Is.EqualTo(2));
        Assert.That(values[0], Is.EqualTo(option1Id.ToString()));
        Assert.That(values[1], Is.EqualTo(option2Id.ToString()));
    }

    [Test]
    public void ConvertAttributes_WhenCheckboxType_ReturnsBoolValue()
    {
        // Arrange
        var attributeId = Guid.NewGuid();

        _tmsAttributes[attributeId] = new TmsAttribute
        {
            Id = attributeId,
            Type = "checkbox"
        };

        var attributes = new List<CaseAttribute>
        {
            new() { Id = attributeId, Value = "true" }
        };

        // Act
        var result = TestBaseWorkItemService.TestConvertAttributes(attributes, _tmsAttributes);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(attributeId));
        Assert.That(result[0].Value, Is.EqualTo(true));
    }

    [Test]
    public void ConvertAttributes_WhenGuidValue_ReturnsUuidPrefix()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var guidValue = Guid.NewGuid();

        _tmsAttributes[attributeId] = new TmsAttribute
        {
            Id = attributeId,
            Type = "string"
        };

        var attributes = new List<CaseAttribute>
        {
            new() { Id = attributeId, Value = guidValue }
        };

        // Act
        var result = TestBaseWorkItemService.TestConvertAttributes(attributes, _tmsAttributes);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(attributeId));
        Assert.That(result[0].Value.ToString(), Is.EqualTo("uuid " + guidValue));
    }

    [Test]
    public void AddAttachmentsToSteps_WhenStepHasAttachments_AddsAttachmentsToStep()
    {
        // Arrange
        var attachmentName = "test.png";
        var attachmentId = Guid.NewGuid();
        var attachments = new Dictionary<string, Guid>
        {
            { attachmentName, attachmentId }
        };

        var steps = new List<Step>
        {
            new()
            {
                Action = "Test action",
                Expected = "Test expected",
                TestData = "Test data",
                ActionAttachments = new List<string> { attachmentName },
                ExpectedAttachments = new List<string> { attachmentName },
                TestDataAttachments = new List<string> { attachmentName }
            }
        };

        // Act
        var result = TestBaseWorkItemService.TestAddAttachmentsToSteps(steps, attachments);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Action, Does.Contain($"/api/Attachments/{attachmentId}"));
        Assert.That(result[0].Expected, Does.Contain($"/api/Attachments/{attachmentId}"));
        Assert.That(result[0].TestData, Does.Contain($"/api/Attachments/{attachmentId}"));
    }

    [Test]
    public void AddAttachmentsToSteps_WhenStepHasNonImageAttachment_AddsFileReference()
    {
        // Arrange
        var attachmentName = "test.doc";
        var attachmentId = Guid.NewGuid();
        var attachments = new Dictionary<string, Guid>
        {
            { attachmentName, attachmentId }
        };

        var steps = new List<Step>
        {
            new()
            {
                Action = "Test action",
                ActionAttachments = new List<string> { attachmentName }
            }
        };

        // Act
        var result = TestBaseWorkItemService.TestAddAttachmentsToSteps(steps, attachments);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Action, Does.Contain($"File attached to test case: {attachmentName}"));
    }

    [Test]
    public void AddAttachmentsToSteps_WhenStepHasInlineImageReference_MovesImageOutsideHtmlTag()
    {
        // Arrange
        var attachmentName = "test.png";
        var attachmentId = Guid.NewGuid();
        var attachments = new Dictionary<string, Guid>
        {
            { attachmentName, attachmentId }
        };

        var steps = new List<Step>
        {
            new()
            {
                Action = $"<p>Some text <<<{attachmentName}>>> more text</p>",
                ActionAttachments = new List<string> { attachmentName }
            }
        };

        // Act
        var result = TestBaseWorkItemService.TestAddAttachmentsToSteps(steps, attachments);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Action, Does.Contain($"<p>Some text  more text</p>"));
        Assert.That(result[0].Action, Does.Contain($"<p> <img src=\"/api/Attachments/{attachmentId}\"> </p>"));
    }
}
