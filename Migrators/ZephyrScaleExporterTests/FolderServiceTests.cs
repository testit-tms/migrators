using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;
using ZephyrScaleExporter.Services;

namespace ZephyrScaleExporterTests;

public class FolderServiceTests
{
    private ILogger<FolderService> _logger;
    private IClient _client;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<FolderService>>();
        _client = Substitute.For<IClient>();
    }

    [Test]
    public async Task ConvertSections_FailedGetFolders()
    {
        // Arrange
        _client.GetFolders()
            .ThrowsAsync(new Exception("Failed to get folders"));

        var service = new FolderService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertSections());
    }

    [Test]
    public async Task ConvertSections_Success()
    {
        // Arrange
        var folders = new List<ZephyrFolder>
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
                Name = "SubFolder 1",
                ParentId = 1
            },
            new()
            {
                Id = 4,
                Name = "SubFolder 2",
                ParentId = 1
            },
            new()
            {
                Id = 5,
                Name = "SubSubFolder 1",
                ParentId = 3
            }
        };

        _client.GetFolders()
            .Returns(folders);

        var service = new FolderService(_logger, _client);

        // Act
        var sectionData = await service.ConvertSections();

        // Assert
        Assert.That(sectionData.Sections, Has.Count.EqualTo(2));
        Assert.That(sectionData.SectionMap, Has.Count.EqualTo(5));
        Assert.That(sectionData.Sections[0].Sections, Has.Count.EqualTo(2));
        Assert.That(sectionData.Sections[0].Sections[0].Sections, Has.Count.EqualTo(1));
        Assert.That(sectionData.Sections[0].Sections[1].Sections, Is.Empty);
        Assert.That(sectionData.Sections[1].Sections, Is.Empty);
        Assert.That(sectionData.Sections[0].Name, Is.EqualTo("Folder 1"));
        Assert.That(sectionData.Sections[0].Sections[0].Name, Is.EqualTo("SubFolder 1"));
        Assert.That(sectionData.Sections[0].Sections[0].Sections[0].Name, Is.EqualTo("SubSubFolder 1"));
        Assert.That(sectionData.Sections[0].Sections[1].Name, Is.EqualTo("SubFolder 2"));
        Assert.That(sectionData.Sections[1].Name, Is.EqualTo("Folder 2"));
    }

    [Test]
    public async Task ConvertSections_OneLayer_Success()
    {
        // Arrange
        var folders = new List<ZephyrFolder>
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
            }
        };

        _client.GetFolders()
            .Returns(folders);

        var service = new FolderService(_logger, _client);

        // Act
        var sectionData = await service.ConvertSections();

        // Assert
        Assert.That(sectionData.Sections, Has.Count.EqualTo(2));
        Assert.That(sectionData.SectionMap, Has.Count.EqualTo(2));
        Assert.That(sectionData.Sections[0].Sections, Is.Empty);
        Assert.That(sectionData.Sections[1].Sections, Is.Empty);
        Assert.That(sectionData.Sections[0].Name, Is.EqualTo("Folder 1"));
        Assert.That(sectionData.Sections[1].Name, Is.EqualTo("Folder 2"));
    }
}
