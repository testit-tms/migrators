using NUnit.Framework;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Helpers;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class UtilsTests
{
    #region ReplaceInvalidChars

    [TestCase("invalid:file/name?.txt", "invalid_file_name_.txt")]
    [TestCase("file<name>|text.txt", "file_name__text.txt")]
    [TestCase("test\"file.txt", "test_file.txt")]
    [TestCase("normal_file.txt", "normal_file.txt")]
    [TestCase("file\tname.txt", "file_name.txt")]
    [TestCase("file\nname.txt", "file_name.txt")]
    [TestCase("", "")]
    [TestCase("a", "a")]
    public void ReplaceInvalidChars_WithDifferentInputs_ReplacesInvalidCharactersCorrectly(string fileName, string expected)
    {
        // Arrange & Act
        var result = Utils.ReplaceInvalidChars(fileName);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ReplaceInvalidChars_WithAllInvalidChars_ReplacesAllWithUnderscores()
    {
        // Arrange
        var invalidChars = Path.GetInvalidFileNameChars();
        var fileName = string.Join("", invalidChars);

        // Act
        var result = Utils.ReplaceInvalidChars(fileName);

        // Assert
        Assert.Multiple(() =>
        {
            foreach (var invalidChar in invalidChars)
            {
                Assert.That(result, Does.Not.Contain(invalidChar));
            }
            Assert.That(result, Is.Not.Empty);
        });
    }

    #endregion

    #region GetLogicalProcessors

    [Test]
    public void GetLogicalProcessors_ReturnsLogicalProcessorCount()
    {
        // Arrange & Act
        var logicalProcessors = Utils.GetLogicalProcessors();

        // Assert
        Assert.That(logicalProcessors, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void GetLogicalProcessors_CalledMultipleTimes_ReturnsCachedValue()
    {
        // Arrange & Act
        var firstCall = Utils.GetLogicalProcessors();
        var secondCall = Utils.GetLogicalProcessors();
        var thirdCall = Utils.GetLogicalProcessors();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(firstCall, Is.EqualTo(secondCall));
            Assert.That(secondCall, Is.EqualTo(thirdCall));
            Assert.That(firstCall, Is.GreaterThanOrEqualTo(1));
        });
    }

    #endregion

    #region SpacesToUnderscores

    [TestCase("hello world", "hello_world")]
    [TestCase("helloworld", "helloworld")]
    [TestCase("hello  world", "hello__world")]
    [TestCase(" hello world ", "_hello_world_")]
    [TestCase("", "")]
    [TestCase(" ", "_")]
    [TestCase("   ", "___")]
    [TestCase("test with multiple spaces", "test_with_multiple_spaces")]
    [TestCase("no_spaces_here", "no_spaces_here")]
    public void SpacesToUnderscores_WithDifferentInputs_ReplacesSpacesCorrectly(string input, string expected)
    {
        // Arrange & Act
        var result = Utils.SpacesToUnderscores(input);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region AddIfUnique - Single Item

    [TestCase(4, true, 4)]
    [TestCase(2, false, 3)]
    [TestCase(1, false, 3)]
    [TestCase(5, true, 4)]
    public void AddIfUnique_WithSingleItem_AddsOnlyIfNotPresent(int item, bool shouldBeAdded, int expectedCount)
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        Utils.AddIfUnique(list, item);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(expectedCount));
            if (shouldBeAdded)
            {
                Assert.That(list, Does.Contain(item));
            }
        });
    }

    [Test]
    public void AddIfUnique_WithEmptyList_AddsItem()
    {
        // Arrange
        var list = new List<int>();
        var item = 1;

        // Act
        Utils.AddIfUnique(list, item);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list, Does.Contain(item));
        });
    }

    [Test]
    public void AddIfUnique_WithStringList_AddsUniqueStrings()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };
        var item = "d";

        // Act
        Utils.AddIfUnique(list, item);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list, Does.Contain(item));
        });
    }

    [Test]
    public void AddIfUnique_WithStringList_DoesNotAddDuplicateStrings()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };
        var item = "b";

        // Act
        Utils.AddIfUnique(list, item);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Count(x => x == "b"), Is.EqualTo(1));
        });
    }

    #endregion

    #region AddIfUnique - List of Items

    [Test]
    public void AddIfUnique_WithListOfItems_AddsAllUniqueItems()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var itemsToAdd = new List<int> { 4, 5, 6 };

        // Act
        Utils.AddIfUnique(list, itemsToAdd);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(6));
            Assert.That(list, Does.Contain(4));
            Assert.That(list, Does.Contain(5));
            Assert.That(list, Does.Contain(6));
        });
    }

    [Test]
    public void AddIfUnique_WithListOfItems_HandlesEmptyTargetList()
    {
        // Arrange
        var list = new List<int>();
        var itemsToAdd = new List<int> { 1, 2, 3 };

        // Act
        Utils.AddIfUnique(list, itemsToAdd);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list, Does.Contain(1));
            Assert.That(list, Does.Contain(2));
            Assert.That(list, Does.Contain(3));
        });
    }

    [Test]
    public void AddIfUnique_WithListOfItems_HandlesStringList()
    {
        // Arrange
        var list = new List<string> { "a", "b" };
        var itemsToAdd = new List<string> { "c", "d", "e" };

        // Act
        Utils.AddIfUnique(list, itemsToAdd);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(5));
            Assert.That(list, Does.Contain("c"));
            Assert.That(list, Does.Contain("d"));
            Assert.That(list, Does.Contain("e"));
        });
    }

    [Test]
    public void AddIfUnique_WithListOfItems_DoesNotAddDuplicateItems()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var itemsToAdd = new List<int> { 2, 3, 4 };

        // Act
        Utils.AddIfUnique(list, itemsToAdd);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list, Does.Contain(4));
            Assert.That(list.Count(x => x == 2), Is.EqualTo(1));
            Assert.That(list.Count(x => x == 3), Is.EqualTo(1));
        });
    }

    [Test]
    public void AddIfUnique_WithListOfItems_HandlesEmptyItemsList()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var itemsToAdd = new List<int>();

        // Act
        Utils.AddIfUnique(list, itemsToAdd);

        // Assert
        Assert.That(list.Count, Is.EqualTo(3));
    }


    [Test]
    public void AddIfUnique_WithListOfItems_HandlesDuplicatesInItemsList()
    {
        // Arrange
        var list = new List<int> { 1, 2 };
        var itemsToAdd = new List<int> { 3, 3, 3, 4 };

        // Act
        Utils.AddIfUnique(list, itemsToAdd);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list.Count(x => x == 3), Is.EqualTo(1));
            Assert.That(list, Does.Contain(4));
        });
    }

    #endregion

    #region ExtractAttachments

    [TestCase(null)]
    [TestCase("")]
    public void ExtractAttachments_WithNullOrEmptyDescription_ReturnsEmptyData(string? description)
    {
        // Arrange & Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Description, Is.EqualTo(string.Empty));
            Assert.That(result.Attachments, Is.Empty);
        });
    }

    [TestCase("<img src=\"../path/to/image.jpg\">", "<<<image.jpg>>>", "image.jpg", "/path/to/image.jpg", TestName = "WithSingleImage_ExtractsAttachmentCorrectly")]
    [TestCase("<img src=\"../path/to/my image file.jpg\">", "<<<my_image_file.jpg>>>", "my_image_file.jpg", "/path/to/my image file.jpg", TestName = "WithImageWithSpacesInFileName_ReplacesSpacesWithUnderscores")]
    [TestCase("<img src=\"../path/to/folder-name/image_file.jpg\">", "<<<image_file.jpg>>>", "image_file.jpg", "/path/to/folder-name/image_file.jpg", TestName = "WithImageWithSpecialCharactersInPath_ExtractsCorrectly")]
    [TestCase("<img src=\"../image.jpg\">", "<<<image.jpg>>>", "image.jpg", "/image.jpg", TestName = "WithImageAtRootPath_ExtractsCorrectly")]
    public void ExtractAttachments_WithSingleImage_ExtractsAttachmentCorrectly(
        string description,
        string expectedDescription,
        string expectedFileName,
        string expectedUrl)
    {
        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Description, Is.EqualTo(expectedDescription));
            Assert.That(result.Attachments, Has.Count.EqualTo(1));
            Assert.That(result.Attachments[0].FileName, Is.EqualTo(expectedFileName));
            Assert.That(result.Attachments[0].Url, Is.EqualTo(expectedUrl));
        });
    }

    [Test]
    public void ExtractAttachments_WithMultipleImages_ExtractsAllAttachments()
    {
        // Arrange
        var description = "<img src=\"../path/to/image1.jpg\">Text<img src=\"../path/to/image2.png\">";
        var expectedDescription = "<<<image1.jpg>>>Text<<<image2.png>>>";

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Description, Is.EqualTo(expectedDescription));
            Assert.That(result.Attachments, Has.Count.EqualTo(2));
            Assert.That(result.Attachments[0].FileName, Is.EqualTo("image1.jpg"));
            Assert.That(result.Attachments[0].Url, Is.EqualTo("/path/to/image1.jpg"));
            Assert.That(result.Attachments[1].FileName, Is.EqualTo("image2.png"));
            Assert.That(result.Attachments[1].Url, Is.EqualTo("/path/to/image2.png"));
        });
    }

    [Test]
    public void ExtractAttachments_WithImageWithoutSrc_DoesNotExtractAttachment()
    {
        // Arrange
        var description = "<img alt=\"test\">";

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Description, Is.EqualTo(description));
            Assert.That(result.Attachments, Is.Empty);
        });
    }

    [Test]
    public void ExtractAttachments_WithImageInComplexHtml_ExtractsCorrectly()
    {
        // Arrange
        var description = "<div><p>Text</p><img src=\"../path/to/image.jpg\"><p>More text</p></div>";

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Description, Does.Contain("<<<image.jpg>>>"));
            Assert.That(result.Attachments, Has.Count.EqualTo(1));
            Assert.That(result.Attachments[0].FileName, Is.EqualTo("image.jpg"));
        });
    }

    [Test]
    public void ExtractAttachments_WithNoImages_ReturnsOriginalDescription()
    {
        // Arrange
        var description = "This is a plain text description without images.";

        // Act
        var result = Utils.ExtractAttachments(description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Description, Is.EqualTo(description));
            Assert.That(result.Attachments, Is.Empty);
        });
    }

    #endregion

    #region ExtractHyperlinks

    [TestCase(null)]
    [TestCase("")]
    public void ExtractHyperlinks_WithNullOrEmptyDescription_ReturnsEmptyString(string? description)
    {
        // Arrange & Act
        var result = Utils.ExtractHyperlinks(description);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [TestCase("<a href=\"http://example.com\">Click here</a>", "[http://example.com]Click here")]
    [TestCase("<a href=\"http://example.com\">Link 1</a>Text<a href=\"https://test.org\">Link 2</a>", "[http://example.com]Link 1Text[https://test.org]Link 2")]
    [TestCase("<div><p><a href=\"http://example.com\">Link</a></p></div>", "[http://example.com]Link")]
    [TestCase("<p>Text</p><a href=\"http://example.com\">Link</a><span>More</span>", "Text[http://example.com]LinkMore")]
    [TestCase("<a href=\"http://example.com\" target=\"_blank\" class=\"link\">Click</a>", "[http://example.com]Click")]
    public void ExtractHyperlinks_WithDifferentHyperlinkFormats_ExtractsAndReplacesCorrectly(string description, string expected)
    {
        // Arrange & Act
        var result = Utils.ExtractHyperlinks(description);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ExtractHyperlinks_WithHyperlinkWithoutHref_DoesNotReplace()
    {
        // Arrange
        var description = "<a>No href link</a>";

        // Act
        var result = Utils.ExtractHyperlinks(description);

        // Assert
        Assert.That(result, Does.Not.Contain("["));
    }

    [Test]
    public void ExtractHyperlinks_WithNoHyperlinks_ReturnsOriginalDescription()
    {
        // Arrange
        var description = "Plain text without hyperlinks.";

        // Act
        var result = Utils.ExtractHyperlinks(description);

        // Assert
        Assert.That(result, Is.EqualTo(description));
    }

    [Test]
    public void ExtractHyperlinks_WithHyperlinkContainingSpacesInUrl_DoesNotExtractUrl()
    {
        // Arrange
        var description = "<a href=\"http://example.com/page with spaces\">Link</a>";

        // Act
        var result = Utils.ExtractHyperlinks(description);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Not.Contain("["));
            Assert.That(result, Does.Contain("Link"));
        });
    }

    #endregion

    #region ConvertingFormatCharacters

    [TestCase(null)]
    [TestCase("")]
    public void ConvertingFormatCharacters_WithNullOrEmptyDescription_ReturnsEmptyString(string? description)
    {
        // Arrange & Act
        var result = Utils.ConvertingFormatCharacters(description);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [TestCase("Line 1\nLine 2\tLine 3", "<p class=\"tiptap-text\">Line 1</p><p class=\"tiptap-text\">Line 2    Line 3</p>")]
    [TestCase("Line 1\nLine 2\nLine 3", "<p class=\"tiptap-text\">Line 1</p><p class=\"tiptap-text\">Line 2</p><p class=\"tiptap-text\">Line 3</p>")]
    [TestCase("Line 1\tLine 2\tLine 3", "<p class=\"tiptap-text\">Line 1    Line 2    Line 3</p>")]
    [TestCase("Plain text without format characters.", "<p class=\"tiptap-text\">Plain text without format characters.</p>")]
    [TestCase("Text\t\tMore\tText", "<p class=\"tiptap-text\">Text        More    Text</p>")]
    [TestCase("Line 1\n\nLine 3", "<p class=\"tiptap-text\">Line 1</p><p class=\"tiptap-text\"></p><p class=\"tiptap-text\">Line 3</p>")]
    [TestCase("Single line", "<p class=\"tiptap-text\">Single line</p>")]
    [TestCase("\tText\t", "<p class=\"tiptap-text\">    Text    </p>")]
    public void ConvertingFormatCharacters_WithDifferentInputs_ConvertsCorrectly(string description, string expected)
    {
        // Arrange & Act
        var result = Utils.ConvertingFormatCharacters(description);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion
}
