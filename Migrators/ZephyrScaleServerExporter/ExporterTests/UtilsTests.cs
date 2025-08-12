using NUnit.Framework;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Helpers;

namespace ExporterTests;

[TestFixture]
public class UtilsTests
{
    [Test]
    public void ReplaceInvalidChars_ShouldReplaceInvalidCharactersWithUnderscores()
    {
        // Arrange
        string fileName = "invalid:file/name?.txt";
        string expected = "invalid_file_name_.txt";

        // Act
        string result = Utils.ReplaceInvalidChars(fileName);

        // Assert
        Assert.That(expected, Is.EqualTo(result));
    }
    
    [Test]
    public void GetLogicalProcessors_ReturnsLogicalProcessorCount()
    {
        // Arrange & Act
        var logicalProcessors = Utils.GetLogicalProcessors();

        // Assert
        Assert.That(logicalProcessors, Is.GreaterThanOrEqualTo(1)); // Minimum 1 processor
    }
    
    [Test]
    public void SpacesToUnderscores_ReplacesSpacesWithUnderscores()
    {
        // Arrange
        var input = "hello world";
        var expected = "hello_world";

        // Act
        var result = Utils.SpacesToUnderscores(input);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void SpacesToUnderscores_ReturnsInputUnchangedIfNoSpaces()
    {
        // Arrange
        var input = "helloworld";

        // Act
        var result = Utils.SpacesToUnderscores(input);

        // Assert
        Assert.That(result, Is.EqualTo(input));
    }
    
    [Test]
    public void AddIfUnique_AddsItemToListIfNotPresent()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var item = 4;

        // Act
        Utils.AddIfUnique(list, item);

        // Assert
        Assert.That(list, Does.Contain(item));
    }

    [Test]
    public void AddIfUnique_DoesNotAddItemToListIfPresent()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var item = 2;

        // Act
        Utils.AddIfUnique(list, item);

        // Assert
        Assert.That(list.Count, Is.EqualTo(3));
    }
    
    [Test]
    public void ExtractAttachments_ExtractsAttachmentsFromDescription()
    {
        // Arrange
        var description = "<img src=\"../path/to/image.jpg\">";
        var expectedDescription = "<<<image.jpg>>>";
        var expectedAttachments = new List<ZephyrAttachment>
        {
            new ZephyrAttachment
            {
                FileName = "image.jpg",
                Url = "/path/to/image.jpg"
            }
        };

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.That(result.Description, Is.EqualTo(expectedDescription));
        Assert.That(result.Attachments, Has.Count.EqualTo(expectedAttachments.Count));
        Assert.That(result.Attachments[0].FileName, Is.EqualTo(expectedAttachments[0].FileName));
        Assert.That(result.Attachments[0].Url, Is.EqualTo(expectedAttachments[0].Url));
    }
    
    [Test]
    public void ExtractHyperlinks_ExtractsAndReplacesHyperlinksWithMarkdown()
    {
        // Arrange
        var description = "<a href=\"http://example.com\">Click here</a>";
        var expected = "[http://example.com]Click here";

        // Act
        var result = Utils.ExtractHyperlinks(description);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
    
    [Test]
    public void ConvertingFormatCharacters_ConvertsNewLinesAndTabs()
    {
        // Arrange
        var description = "Line 1\nLine 2\tLine 3";
        var expected = "<p class=\"tiptap-text\">Line 1</p><p class=\"tiptap-text\">Line 2    Line 3</p>";

        // Act
        var result = Utils.ConvertingFormatCharacters(description);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
}