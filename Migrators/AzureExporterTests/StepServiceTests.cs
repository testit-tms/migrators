using AzureExporter.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using Models;
using NSubstitute;

namespace AzureExporterTests;

public class StepServiceTests
{
    private ILogger<StepService> _logger;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<StepService>>();
    }

    [Test]
    public void ConvertSteps_ShouldReturnEmptyList_WhenStepsContentIsNull()
    {
        // Arrange
        var stepService = new StepService(_logger);

        // Act
        var result = stepService.ConvertSteps(null, new Dictionary<int, Guid>());

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ConvertSteps_ShouldReturnEmptyList_WhenStepsContentIsEmpty()
    {
        // Arrange
        var stepService = new StepService(_logger);

        // Act
        var result = stepService.ConvertSteps(string.Empty, new Dictionary<int, Guid>());

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ConvertSteps_ShouldReturnEmptyList_WhenStepsContentIsWhitespace()
    {
        // Arrange
        var stepService = new StepService(_logger);

        // Act
        var result = stepService.ConvertSteps(" ", new Dictionary<int, Guid>());

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ConvertSteps_ShouldReturnException_WhenStepsContentIsInvalidXml()
    {
        // Arrange
        var stepService = new StepService(_logger);

        // Act
        Assert.Throws<InvalidOperationException>(() =>
            stepService.ConvertSteps("<xml></xml>", new Dictionary<int, Guid>()));
    }

    [Test]
    public void ConvertSteps_ShouldReturnSteps_WhenStepsContentHasOnlySteps()
    {
        // Arrange
        var stepService = new StepService(_logger);
        var context = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<steps id=""0"" last=""4"">
    <step id=""1"" type=""ValidateStep"">
        <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;Step01&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
        <parameterizedString isformatted=""true"">&lt;P&gt;ExpectedResult01&lt;/P&gt;</parameterizedString>
        <description/>
    </step>
    <step id=""2"" type=""ValidateStep"">
        <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;DIV&gt;&lt;P&gt;Step02&lt;/P&gt;&lt;/DIV&gt;&lt;/DIV&gt;</parameterizedString>
        <parameterizedString isformatted=""true"">&lt;P&gt;ExpectedResult02&lt;/P&gt;</parameterizedString>
        <description/>
    </step>
</steps>
";
        var expectedSteps = new List<Step>
        {
            new()
            {
                Action = "<DIV><P>Step01</P></DIV>",
                Expected = "<P>ExpectedResult01</P>",
                Attachments = new List<string>(),
                SharedStepId = null
            },
            new()
            {
                Action = "<DIV><DIV><P>Step02</P></DIV></DIV>",
                Expected = "<P>ExpectedResult02</P>",
                Attachments = new List<string>(),
                SharedStepId = null
            }
        };

        // Act
        var result = stepService.ConvertSteps(context, new Dictionary<int, Guid>());

        // Assert
        Assert.That(result[0].Action, Is.EqualTo(expectedSteps[0].Action));
        Assert.That(result[0].Expected, Is.EqualTo(expectedSteps[0].Expected));
        Assert.That(result[0].Attachments, Is.EqualTo(expectedSteps[0].Attachments));
        Assert.That(result[0].SharedStepId, Is.EqualTo(expectedSteps[0].SharedStepId));
        Assert.That(result[1].Action, Is.EqualTo(expectedSteps[1].Action));
        Assert.That(result[1].Expected, Is.EqualTo(expectedSteps[1].Expected));
        Assert.That(result[1].Attachments, Is.EqualTo(expectedSteps[1].Attachments));
        Assert.That(result[1].SharedStepId, Is.EqualTo(expectedSteps[1].SharedStepId));
    }

    [Test]
    public void ConvertSteps_ShouldReturnSteps_WhenStepsContentHasOnlyStepsAndSharedStepsMap()
    {
        // Arrange
        var stepService = new StepService(_logger);
        var context = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<steps id=""0"" last=""4"">
    <step id=""1"" type=""ValidateStep"">
        <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;Step01&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
        <parameterizedString isformatted=""true"">&lt;P&gt;ExpectedResult01&lt;/P&gt;</parameterizedString>
        <description/>
    </step>
    <step id=""2"" type=""ValidateStep"">
        <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;DIV&gt;&lt;P&gt;Step02&lt;/P&gt;&lt;/DIV&gt;&lt;/DIV&gt;</parameterizedString>
        <parameterizedString isformatted=""true"">&lt;P&gt;ExpectedResult02&lt;/P&gt;</parameterizedString>
        <description/>
    </step>
</steps>
";
        var expectedSteps = new List<Step>
        {
            new()
            {
                Action = "<DIV><P>Step01</P></DIV>",
                Expected = "<P>ExpectedResult01</P>",
                Attachments = new List<string>(),
                SharedStepId = null
            },
            new()
            {
                Action = "<DIV><DIV><P>Step02</P></DIV></DIV>",
                Expected = "<P>ExpectedResult02</P>",
                Attachments = new List<string>(),
                SharedStepId = null
            }
        };

        // Act
        var result = stepService.ConvertSteps(context, new Dictionary<int, Guid>()
        {
            { 1, Guid.NewGuid() }
        });

        // Assert
        Assert.That(result[0].Action, Is.EqualTo(expectedSteps[0].Action));
        Assert.That(result[0].Expected, Is.EqualTo(expectedSteps[0].Expected));
        Assert.That(result[0].Attachments, Is.EqualTo(expectedSteps[0].Attachments));
        Assert.That(result[0].SharedStepId, Is.EqualTo(expectedSteps[0].SharedStepId));
        Assert.That(result[1].Action, Is.EqualTo(expectedSteps[1].Action));
        Assert.That(result[1].Expected, Is.EqualTo(expectedSteps[1].Expected));
        Assert.That(result[1].Attachments, Is.EqualTo(expectedSteps[1].Attachments));
        Assert.That(result[1].SharedStepId, Is.EqualTo(expectedSteps[1].SharedStepId));
    }

    [Test]
    public void ConvertSteps_ShouldReturnSteps_WhenStepsContentHasStepsAndSharedSteps()
    {
        // Arrange
        var stepService = new StepService(_logger);
        var sharedStepId = Guid.NewGuid();
        var context = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<steps id=""0"" last=""7"">
    <step id=""2"" type=""ValidateStep"">
        <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;Step01&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
        <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;ExpectedResult01&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
        <description/>
    </step>
    <compref id=""6"" ref=""1"">
        <step id=""5"" type=""ValidateStep"">
            <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;Step03&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
            <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;ExpectedResult03&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
            <description/>
        </step>
        <step id=""7"" type=""ActionStep"">
            <parameterizedString isformatted=""true"">&lt;P&gt;Step04&lt;/P&gt;</parameterizedString>
            <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;&lt;BR/&gt;&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
            <description/>
        </step>
    </compref>
</steps>
";
        var expectedSteps = new List<Step>
        {
            new()
            {
                Action = "<DIV><P>Step01</P></DIV>",
                Expected = "<DIV><P>ExpectedResult01</P></DIV>",
                Attachments = new List<string>(),
                SharedStepId = null
            },
            new()
            {
                Action = string.Empty,
                Expected = string.Empty,
                Attachments = new List<string>(),
                SharedStepId = sharedStepId
            },
            new()
            {
                Action = "<DIV><P>Step03</P></DIV>",
                Expected = "<DIV><P>ExpectedResult03</P></DIV>",
                Attachments = new List<string>(),
                SharedStepId = null
            },
            new()
            {
                Action = "<P>Step04</P>",
                Expected = "<DIV><P><BR/></P></DIV>",
                Attachments = new List<string>(),
                SharedStepId = null
            }
        };

        // Act
        var result = stepService.ConvertSteps(context, new Dictionary<int, Guid>()
        {
            { 1, sharedStepId }
        });

        // Assert
        Assert.That(result[0].Action, Is.EqualTo(expectedSteps[0].Action));
        Assert.That(result[0].Expected, Is.EqualTo(expectedSteps[0].Expected));
        Assert.That(result[0].Attachments, Is.EqualTo(expectedSteps[0].Attachments));
        Assert.That(result[0].SharedStepId, Is.EqualTo(expectedSteps[0].SharedStepId));
        Assert.That(result[1].Action, Is.EqualTo(expectedSteps[1].Action));
        Assert.That(result[1].Expected, Is.EqualTo(expectedSteps[1].Expected));
        Assert.That(result[1].Attachments, Is.EqualTo(expectedSteps[1].Attachments));
        Assert.That(result[1].SharedStepId, Is.EqualTo(expectedSteps[1].SharedStepId));
        Assert.That(result[2].Action, Is.EqualTo(expectedSteps[2].Action));
        Assert.That(result[2].Expected, Is.EqualTo(expectedSteps[2].Expected));
        Assert.That(result[2].Attachments, Is.EqualTo(expectedSteps[2].Attachments));
        Assert.That(result[2].SharedStepId, Is.EqualTo(expectedSteps[2].SharedStepId));
        Assert.That(result[3].Action, Is.EqualTo(expectedSteps[3].Action));
        Assert.That(result[3].Expected, Is.EqualTo(expectedSteps[3].Expected));
        Assert.That(result[3].Attachments, Is.EqualTo(expectedSteps[3].Attachments));
        Assert.That(result[3].SharedStepId, Is.EqualTo(expectedSteps[3].SharedStepId));
    }

    [Test]
    public void ConvertSteps_ShouldReturnException_WhenStepsContentHasStepsAndSharedStepsWithOutSharedMap()
    {
        // Arrange
        var stepService = new StepService(_logger);
        var sharedStepId = Guid.NewGuid();
        var context = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<steps id=""0"" last=""7"">
    <step id=""2"" type=""ValidateStep"">
        <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;Step01&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
        <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;ExpectedResult01&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
        <description/>
    </step>
    <compref id=""6"" ref=""1"">
        <step id=""5"" type=""ValidateStep"">
            <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;Step03&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
            <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;ExpectedResult03&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
            <description/>
        </step>
        <step id=""7"" type=""ActionStep"">
            <parameterizedString isformatted=""true"">&lt;P&gt;Step04&lt;/P&gt;</parameterizedString>
            <parameterizedString isformatted=""true"">&lt;DIV&gt;&lt;P&gt;&lt;BR/&gt;&lt;/P&gt;&lt;/DIV&gt;</parameterizedString>
            <description/>
        </step>
    </compref>
</steps>
";

        // Act
        Assert.Throws<Exception>(() => stepService.ConvertSteps(context, new Dictionary<int, Guid>()));
    }
}
