using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TestCollabExporter.Client;
using TestCollabExporter.Models;
using TestCollabExporter.Services;

namespace TestCollabExporterTests;

public class SectionServiceTests
{
    private ILogger<SectionService> _logger;
    private IClient _client;

    private const int ProjectId = 1;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SectionService>>();
        _client = Substitute.For<IClient>();
    }

    [Test]
    public async Task ConvertSections_FailedGetSuites()
    {
        // Arrange
        _client.GetSuites(ProjectId)
            .Throws(new Exception("Failed to get suites"));

        var sectionService = new SectionService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await sectionService.ConvertSections(ProjectId));
    }

    [Test]
    public async Task ConvertSections_Success()
    {
        // Arrange
        var suites = new List<TestCollabSuite>
        {
            new()
            {
                Id = 1,
                Parent_id = 0,
                Title = "Suite 1"
            },
            new()
            {
                Id = 2,
                Parent_id = 0,
                Title = "Suite 2"
            },
            new()
            {
                Id = 3,
                Parent_id = 2,
                Title = "Suite 3"
            }
        };

        _client.GetSuites(ProjectId)
            .Returns(suites);

        var sectionService = new SectionService(_logger, _client);

        // Act
        var result = await sectionService.ConvertSections(ProjectId);

        // Assert
        Assert.That(result.Sections, Has.Count.EqualTo(2));
        Assert.That(result.SectionMap, Has.Count.EqualTo(3));
        Assert.That(result.Sections[0].Name, Is.EqualTo("Suite 1"));
        Assert.That(result.Sections[1].Name, Is.EqualTo("Suite 2"));
        Assert.That(result.Sections[1].Sections, Has.Count.EqualTo(1));
        Assert.That(result.Sections[1].Sections[0].Name, Is.EqualTo("Suite 3"));
    }
}
