

using ZephyrScaleExporter.Services;

namespace ZephyrScaleExporterTests;

public class UtilsTests
{
    [Test]
    public void ExtractAttachments_ShouldReturnEmptyAttachments_WhenDescriptionIsNull()
    {
        // Arrange
        const string? description = (string?)null;

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.That(result.Description, Is.EqualTo(string.Empty));
        Assert.That(result.Attachments, Is.Empty);
    }

    [Test]
    public void ExtractAttachments_ShouldReturnEmptyAttachments_WhenDescriptionIsEmpty()
    {
        // Arrange
        const string? description = "";

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.That(result.Description, Is.EqualTo(string.Empty));
        Assert.That(result.Attachments, Is.Empty);
    }

    [Test]
    public void ExtractAttachments_ShouldReturnEmptyAttachments_WhenDescriptionDoesNotContainImages()
    {
        // Arrange
        const string? description = "This is a description without images";

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.That(result.Description, Is.EqualTo(description));
        Assert.That(result.Attachments, Is.Empty);
    }

    [Test]
    public void ExtractAttachments_ShouldReturnAttachments_WhenDescriptionContainsImages()
    {
        // Arrange
        const string? description = "This is a description with an image <img src=\"https://www.example.com/image.png\" />";

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.That(result.Description, Is.EqualTo("This is a description with an image <<<image.png>>>"));
        Assert.That(result.Attachments, Is.Not.Empty);
        Assert.That(result.Attachments.Count, Is.EqualTo(1));
        Assert.That(result.Attachments[0].FileName, Is.EqualTo("image.png"));
        Assert.That(result.Attachments[0].Url, Is.EqualTo("https://www.example.com/image.png"));
    }

    [Test]
    public void ExtractAttachments_ShouldReturnAttachments_WhenDescriptionContainsMultipleImages()
    {
        // Arrange
        const string? description = "This is a description with an image <img src=\"https://www.example.com/image.png\" /> and another image <img src=\"https://www.example.com/image2.png\" />";

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.That(result.Description, Is.EqualTo("This is a description with an image <<<image.png>>> and another image <<<image2.png>>>"));
        Assert.That(result.Attachments, Is.Not.Empty);
        Assert.That(result.Attachments.Count, Is.EqualTo(2));
        Assert.That(result.Attachments[0].FileName, Is.EqualTo("image.png"));
        Assert.That(result.Attachments[0].Url, Is.EqualTo("https://www.example.com/image.png"));
        Assert.That(result.Attachments[1].FileName, Is.EqualTo("image2.png"));
        Assert.That(result.Attachments[1].Url, Is.EqualTo("https://www.example.com/image2.png"));
    }
}
