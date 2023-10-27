using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using XRayExporter.Client;
using XRayExporter.Models;
using XRayExporter.Services;

namespace XRayExporterTests;

public class SectionServiceTests
{
    private ILogger<SectionService> _logger;
    private IClient _client;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SectionService>>();
        _client = Substitute.For<IClient>();
    }

    [Test]
    public async Task ConvertSections_WhenNoFolders_ReturnsEmptyList()
    {
        // Arrange
        _client.GetFolders().Returns(new List<XrayFolder>());

        var sectionService = new SectionService(_logger, _client);

        // Act
        var result = await sectionService.ConvertSections();

        // Assert
        Assert.That(result.Sections, Is.Empty);
    }

    [Test]
    public async Task ConvertSections_WhenFolders_ReturnsSections()
    {
        // Arrange
        var folders = new List<XrayFolder>
        {
            new()
            {
                Id = 1,
                Name = "Folder 1",
                Folders = new List<XrayFolder>
                {
                    new()
                    {
                        Id = 2,
                        Name = "Folder 1.1",
                        Folders = new List<XrayFolder>
                        {
                            new()
                            {
                                Id = 3,
                                Name = "Folder 1.1.1",
                                Folders = new List<XrayFolder>()
                            }
                        }
                    },
                    new()
                    {
                        Id = 4,
                        Name = "Folder 1.2",
                        Folders = new List<XrayFolder>()
                    }
                }
            },
            new()
            {
                Id = 5,
                Name = "Folder 2",
                Folders = new List<XrayFolder>()
            }
        };

        _client.GetFolders().Returns(folders);

        var sectionService = new SectionService(_logger, _client);

        // Act
        var result = await sectionService.ConvertSections();

        // Assert
        Assert.That(result.Sections, Has.Count.EqualTo(2));
        Assert.That(result.Sections[0].Name, Is.EqualTo("Folder 1"));
        Assert.That(result.Sections[0].Sections, Has.Count.EqualTo(2));
        Assert.That(result.Sections[0].Sections[0].Name, Is.EqualTo("Folder 1.1"));
        Assert.That(result.Sections[0].Sections[0].Sections, Has.Count.EqualTo(1));
        Assert.That(result.Sections[0].Sections[0].Sections[0].Name, Is.EqualTo("Folder 1.1.1"));
        Assert.That(result.Sections[0].Sections[0].Sections[0].Sections, Is.Empty);
        Assert.That(result.Sections[0].Sections[1].Name, Is.EqualTo("Folder 1.2"));
        Assert.That(result.Sections[0].Sections[1].Sections, Is.Empty);
        Assert.That(result.Sections[1].Name, Is.EqualTo("Folder 2"));
        Assert.That(result.Sections[1].Sections, Is.Empty);
        Assert.That(result.SectionMap, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task ConvertSections_FailedGetFolders()
    {
        // Arrange
        _client.GetFolders()
            .Throws(new Exception("Failed to get folders"));

        var sectionService = new SectionService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await sectionService.ConvertSections());
    }

}
