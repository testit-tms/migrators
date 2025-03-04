using Microsoft.Extensions.Logging;
using Importer.Client;
using Importer.Services.Implementations;
using NSubstitute;

namespace ImporterTests;

public class ProjectServiceTests
{
    private ILogger<ProjectService> _logger = null!;
    private IClientAdapter _clientAdapter = null!;
    private ProjectService _projectService = null!;

    private const string ProjectName = "Test Project";
    private static readonly Guid ExistingProjectId = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private static readonly Guid NewProjectId = Guid.Parse("9f3c5ed5-d7d4-483f-b69f-e68c079cffe8");

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ProjectService>>();
        _clientAdapter = Substitute.For<IClientAdapter>();
        _projectService = new ProjectService(_logger, _clientAdapter);
    }

    [Test]
    public async Task ImportProject_WhenProjectExists_ReturnsExistingId()
    {
        // Arrange
        _clientAdapter.GetProject(ProjectName).Returns(ExistingProjectId);

        // Act
        var result = await _projectService.ImportProject(ProjectName);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Is.EqualTo(ExistingProjectId));
            await _clientAdapter.Received(1).GetProject(ProjectName);
            await _clientAdapter.DidNotReceive().CreateProject(Arg.Any<string>());
        });
    }

    [Test]
    public async Task ImportProject_WhenProjectNotExists_CreatesNewProject()
    {
        // Arrange
        _clientAdapter.GetProject(ProjectName).Returns(Guid.Empty);
        _clientAdapter.CreateProject(ProjectName).Returns(NewProjectId);

        // Act
        var result = await _projectService.ImportProject(ProjectName);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Is.EqualTo(NewProjectId));
            await _clientAdapter.Received(1).GetProject(ProjectName);
            await _clientAdapter.Received(1).CreateProject(ProjectName);
        });
    }
} 