using System.Text.Json;
using Importer.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using Attribute = Models.Attribute;

namespace ImporterTests;

public class ParserServiceTests : IDisposable
{
    private ILogger<ParserService> _logger = null!;
    private IConfiguration _configuration = null!;
    private ParserService _parserService = null!;
    private string _testPath = null!;

    private static readonly Guid WorkItemId = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private const string TestFileName = "test.txt";
    private const string MainJson = "main.json";
    private const string TestCaseJson = "testCase.json";
    private const string SharedStepJson = "sharedStep.json";

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ParserService>>();
        _configuration = Substitute.For<IConfiguration>();

        // temp directory
        _testPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_testPath);
        Directory.CreateDirectory(Path.Combine(_testPath, WorkItemId.ToString()));

        _configuration["resultPath"].Returns(_testPath);

        _parserService = new ParserService(_logger, _configuration);
    }

    [Test]
    public void Constructor_WhenResultPathNotSet_ThrowsArgumentException()
    {
        // Arrange
        _configuration["resultPath"].Returns((string?)null);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new ParserService(_logger, _configuration));
        Assert.That(ex!.Message, Is.EqualTo("resultPath is not set"));
    }

    [Test]
    public async Task GetMainFile_WhenFileNotExists_ThrowsFileNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(async () => await _parserService.GetMainFile());
        Assert.That(ex!.Message, Is.EqualTo("Main json file not found"));
    }

    [Test]
    public async Task GetMainFile_WhenFileEmpty_ThrowsApplicationException()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testPath, MainJson), "null");

        // Act & Assert
        var ex = Assert.ThrowsAsync<ApplicationException>(async () => await _parserService.GetMainFile());
        Assert.That(ex!.Message, Is.EqualTo("Main json file is empty"));
    }

    [Test]
    public async Task GetMainFile_WhenInvalidJson_ThrowsJsonException()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testPath, MainJson), "{invalid json}");

        // Act & Assert
        Assert.ThrowsAsync<JsonException>(async () => await _parserService.GetMainFile());
    }

    [Test]
    public async Task GetMainFile_WhenFileValid_ReturnsRoot()
    {
        // Arrange
        var root = new Root
        {
            ProjectName = "Test Project",
            Sections = new List<Section> { new() { Id = Guid.NewGuid() } },
            Attributes = new List<Attribute> { new() { Id = Guid.NewGuid() } },
            SharedSteps = new List<Guid> { Guid.NewGuid() },
            TestCases = new List<Guid> { Guid.NewGuid() }
        };

        await File.WriteAllTextAsync(
            Path.Combine(_testPath, MainJson),
            JsonSerializer.Serialize(root)
        );

        // Act
        var result = await _parserService.GetMainFile();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ProjectName, Is.EqualTo(root.ProjectName));
            Assert.That(result.Sections.Count, Is.EqualTo(root.Sections.Count));
            Assert.That(result.Attributes.Count, Is.EqualTo(root.Attributes.Count));
            Assert.That(result.SharedSteps.Count, Is.EqualTo(root.SharedSteps.Count));
            Assert.That(result.TestCases.Count, Is.EqualTo(root.TestCases.Count));
        });
    }

    [Test]
    public async Task GetSharedStep_WhenFileNotExists_ThrowsApplicationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ApplicationException>(
            async () => await _parserService.GetSharedStep(WorkItemId));
        Assert.That(ex!.Message, Is.EqualTo("Shared step file not found"));
    }

    [Test]
    public async Task GetSharedStep_WhenFileEmpty_ThrowsApplicationException()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, WorkItemId.ToString(), SharedStepJson);
        await File.WriteAllTextAsync(filePath, "null");

        // Act & Assert
        var ex = Assert.ThrowsAsync<ApplicationException>(
            async () => await _parserService.GetSharedStep(WorkItemId));
        Assert.That(ex!.Message, Is.EqualTo("Shared step file is empty"));
    }

    [Test]
    public async Task GetSharedStep_WhenInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, WorkItemId.ToString(), SharedStepJson);
        await File.WriteAllTextAsync(filePath, "{invalid json}");

        // Act & Assert
        Assert.ThrowsAsync<JsonException>(async () => await _parserService.GetSharedStep(WorkItemId));
    }

    [Test]
    public async Task GetSharedStep_WhenFileValid_ReturnsSharedStep()
    {
        // Arrange
        var sharedStep = new SharedStep
        {
            Id = WorkItemId,
            Name = "Test Shared Step",
            SectionId = Guid.NewGuid(),
            Steps = new List<Step> { new() { Action = "Test Action" } }
        };

        var filePath = Path.Combine(_testPath, WorkItemId.ToString(), SharedStepJson);
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(sharedStep));

        // Act
        var result = await _parserService.GetSharedStep(WorkItemId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(sharedStep.Id));
            Assert.That(result.Name, Is.EqualTo(sharedStep.Name));
            Assert.That(result.SectionId, Is.EqualTo(sharedStep.SectionId));
            Assert.That(result.Steps.Count, Is.EqualTo(sharedStep.Steps.Count));
        });
    }

    [Test]
    public async Task GetTestCase_WhenFileNotExists_ThrowsApplicationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ApplicationException>(
            async () => await _parserService.GetTestCase(WorkItemId));
        Assert.That(ex!.Message, Is.EqualTo("Test case file not found"));
    }

    [Test]
    public async Task GetTestCase_WhenFileEmpty_ThrowsApplicationException()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, WorkItemId.ToString(), TestCaseJson);
        await File.WriteAllTextAsync(filePath, "null");

        // Act & Assert
        var ex = Assert.ThrowsAsync<ApplicationException>(
            async () => await _parserService.GetTestCase(WorkItemId));
        Assert.That(ex!.Message, Is.EqualTo("Test case file is empty"));
    }

    [Test]
    public async Task GetTestCase_WhenInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, WorkItemId.ToString(), TestCaseJson);
        await File.WriteAllTextAsync(filePath, "{invalid json}");

        // Act & Assert
        Assert.ThrowsAsync<JsonException>(async () => await _parserService.GetTestCase(WorkItemId));
    }

    [Test]
    public async Task GetTestCase_WhenFileValid_ReturnsTestCase()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = WorkItemId,
            Name = "Test Case",
            SectionId = Guid.NewGuid(),
            Steps = new List<Step> { new() { Action = "Test Action" } }
        };

        var filePath = Path.Combine(_testPath, WorkItemId.ToString(), TestCaseJson);
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(testCase));

        // Act
        var result = await _parserService.GetTestCase(WorkItemId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(testCase.Id));
            Assert.That(result.Name, Is.EqualTo(testCase.Name));
            Assert.That(result.SectionId, Is.EqualTo(testCase.SectionId));
            Assert.That(result.Steps.Count, Is.EqualTo(testCase.Steps.Count));
        });
    }

    [Test]
    public async Task GetAttachment_WhenFileNotExists_ThrowsApplicationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ApplicationException>(
            async () => await _parserService.GetAttachment(WorkItemId, TestFileName));
        Assert.That(ex!.Message, Is.EqualTo("Attachment file not found"));
    }

    [Test]
    public async Task GetAttachment_WhenFileSizeSmall_ReturnsFileStream()
    {
        // Arrange
        var filePath = Path.Combine(_testPath, WorkItemId.ToString(), TestFileName);
        await File.WriteAllTextAsync(filePath, "Test content");

        // Act
        var result = await _parserService.GetAttachment(WorkItemId, TestFileName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Does.EndWith(TestFileName));
            Assert.That(result.Length, Is.GreaterThan(0));
        });
    }


    public void Dispose()
    {
        // Clean folder
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}
