using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpiraTestExporter.Client;
using SpiraTestExporter.Models;
using SpiraTestExporter.Services;

namespace SpiraTestExporterTests;

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
    public async Task GetSections_FailedGetFolders()
    {
        // Arrange
        _client.GetFolders(ProjectId)
            .Throws(new Exception("Test Exception"));

        var sectionService = new SectionService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await sectionService.GetSections(ProjectId));
    }

    [Test]
    public async Task GetSections_Success()
    {
        // Arrange
        var folders = new List<SpiraFolder>
        {
            new()
            {
                Id = 1,
                Name = "Folder 1",
                ParentId = null
            },
            new()
            {
                Id = 2,
                Name = "Folder 2",
                ParentId = null
            },
            new()
            {
                Id = 3,
                Name = "Folder 3",
                ParentId = 1
            },
            new()
            {
                Id = 4,
                Name = "Folder 4",
                ParentId = 1
            }
        };

        _client.GetFolders(ProjectId)
            .Returns(folders);

        var sectionService = new SectionService(_logger, _client);

        // Act
        var sectionData =
            await sectionService.GetSections(ProjectId);

        // Assert
        Assert.That(sectionData.Sections, Has.Count.EqualTo(2));
        Assert.That(sectionData.Sections[0].Sections, Has.Count.EqualTo(2));
        Assert.That(sectionData.SectionMap, Has.Count.EqualTo(4));
    }
}
