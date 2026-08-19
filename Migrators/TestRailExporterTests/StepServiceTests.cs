using Microsoft.Extensions.Logging;
using NSubstitute;
using TestRailExporter.Client;
using TestRailExporter.Models.Client;
using TestRailExporter.Models.Commons;
using TestRailExporter.Services;
using TestRailExporter.Services.Implementations;

namespace TestRailExporterTests;

public class StepServiceTests
{
    [Test]
    public void FormatStepText_ConvertsWikiTable()
    {
        var html = StepService.FormatStepText("|| A || B\n|| 1 || 2\n");

        Assert.That(html, Does.Contain("<table"));
        Assert.That(html, Does.Contain("A"));
        Assert.That(html, Does.Contain("1"));
    }

    [Test]
    public void FormatStepText_ConvertsMarkdownTable()
    {
        var html = StepService.FormatStepText("| H1 | H2 |\n| --- | --- |\n| c1 | c2 |\n");

        Assert.That(html, Does.Contain("<table"));
        Assert.That(html, Does.Contain("H1"));
        Assert.That(html, Does.Contain("c2"));
        Assert.That(html, Does.Not.Contain("---"));
    }

    [Test]
    public void FormatStepText_ConvertsHyperlink()
    {
        var html = StepService.FormatStepText("See [docs](https://example.com)");

        Assert.That(html, Does.Contain("<a target=\"_blank\""));
        Assert.That(html, Does.Contain("href=\"https://example.com\""));
        Assert.That(html, Does.Contain("docs"));
    }

    [Test]
    public void FormatStepText_EscapesXmlAndKeepsAnchor()
    {
        var html = StepService.FormatStepText(
            "<SOAP-ENV:Envelope xmlns:x=\"y\"></SOAP-ENV:Envelope> <a href=\"https://example.com\">ok</a>");

        Assert.That(html, Does.Contain("&lt;SOAP-ENV:Envelope"));
        Assert.That(html, Does.Contain("<a href=\"https://example.com\">ok</a>"));
    }

    [Test]
    public void EscapeUnknownXml_KeepsAttachmentPlaceholder()
    {
        var html = StepService.EscapeUnknownXml("before <<<file.png>>> after");

        Assert.That(html, Is.EqualTo("before <<<file.png>>> after"));
    }

    [Test]
    public async Task ConvertSteps_ExtractsImageWithAltText()
    {
        var logger = Substitute.For<ILogger<StepService>>();
        var client = Substitute.For<IClient>();
        var attachments = Substitute.For<IAttachmentService>();
        var service = new StepService(logger, client, attachments);
        var testCase = new TestRailCase
        {
            Title = "Case",
            TextSteps = "![shot](index.php?/attachments/get/123)"
        };
        var info = new AttachmentsInfo
        {
            AttachmentNames = ["file.png"],
            AttachmentsMap = new Dictionary<string, string> { ["123"] = "file.png" }
        };

        var steps = await service.ConvertStepsForTestCase(testCase, Guid.NewGuid(), [], info);

        Assert.That(steps, Has.Count.EqualTo(1));
        Assert.That(steps[0].Action, Does.Contain("<<<file.png>>>"));
        Assert.That(steps[0].ActionAttachments, Contains.Item("file.png"));
    }
}
