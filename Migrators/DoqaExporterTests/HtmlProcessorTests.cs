using DoqaExporter.Services;
using Models;

namespace DoqaExporterTests;

public class HtmlProcessorTests
{
    [Test]
    public void Process_NullOrEmpty_ReturnsEmpty()
    {
        var counter = 0;
        var result = HtmlProcessor.Process(null, "p", ref counter);

        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Images, Is.Empty);
        Assert.That(counter, Is.EqualTo(0));
    }

    [Test]
    public void Process_StripsHtmlAndKeepsLineBreaks()
    {
        var counter = 0;
        var result = HtmlProcessor.Process("<p>Hello</p><br/><p>World</p>", "p", ref counter);

        Assert.That(result.Text, Does.Contain("Hello"));
        Assert.That(result.Text, Does.Contain("World"));
        Assert.That(result.Text, Does.Not.Contain("<p>"));
    }

    [Test]
    public void Process_ExtractsBase64Image()
    {
        var png1x1 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        var html = $"<img src=\"data:image/png;base64,{png1x1}\" />";
        var counter = 0;

        var result = HtmlProcessor.Process(html, "step1_action", ref counter);

        Assert.That(result.Images, Has.Count.EqualTo(1));
        Assert.That(result.Images[0].FileName, Is.EqualTo("step1_action_1.png"));
        Assert.That(result.Images[0].Data, Is.Not.Empty);
        Assert.That(result.Text, Does.Contain("[Изображение: step1_action_1.png]"));
        Assert.That(counter, Is.EqualTo(1));
    }

    [Test]
    public void Process_ConvertsAnchorToText()
    {
        var counter = 0;
        var result = HtmlProcessor.Process(
            "<a href=\"https://example.com\">Click</a>", "p", ref counter);

        Assert.That(result.Text, Is.EqualTo("Click (https://example.com)"));
    }

    [Test]
    public void ExtractLinks_Empty_ReturnsEmpty()
    {
        Assert.That(HtmlProcessor.ExtractLinks(null), Is.Empty);
        Assert.That(HtmlProcessor.ExtractLinks(""), Is.Empty);
    }

    [Test]
    public void ExtractLinks_TrackerIsIssue()
    {
        var links = HtmlProcessor.ExtractLinks("see https://tracker.yandex.ru/TEST-42 for details");

        Assert.That(links, Has.Count.EqualTo(1));
        Assert.That(links[0].Type, Is.EqualTo(LinkType.Issue));
        Assert.That(links[0].Title, Is.EqualTo("TEST-42"));
        Assert.That(links[0].Url, Is.EqualTo("https://tracker.yandex.ru/TEST-42"));
    }

    [Test]
    public void ExtractLinks_RelatedLinksAndDedup()
    {
        var text =
            "https://www.figma.com/file/abc " +
            "https://docs.google.com/document/d/1 " +
            "https://wiki.yandex.ru/page " +
            "https://www.figma.com/file/abc";

        var links = HtmlProcessor.ExtractLinks(text);

        Assert.That(links, Has.Count.EqualTo(3));
        Assert.That(links.All(l => l.Type == LinkType.Related), Is.True);
        Assert.That(links.Select(l => l.Title), Is.EquivalentTo(new[] { "Figma", "Google Docs", "Wiki" }));
    }
}
