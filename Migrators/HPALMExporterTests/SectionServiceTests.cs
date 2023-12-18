using HPALMExporter.Client;
using HPALMExporter.Services;
using ImportHPALMToTestIT.Models.HPALM;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HPALMExporterTests;

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
    public async Task ConvertSections_FailedGetTestFolders()
    {
        // Arrange
        _client.GetTestFolders(0)
            .Throws(new Exception("Failed to get test folders"));

        var service = new SectionService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(() => service.ConvertSections());
    }

    [Test]
    public async Task ConvertSections_Success()
    {
        // Arrange
        var rootFolders = new List<HPALMFolder>
        {
            new()
            {
                Id = 1,
                Name = "Folder 1",
                ParentId = 0
            }
        };

        var childFolders = new List<HPALMFolder>
        {
            new()
            {
                Id = 3,
                Name = "Subfolder 1",
                ParentId = 0
            }
        };

        _client.GetTestFolders(0)
            .Returns(rootFolders);

        _client.GetTestFolders(1)
            .Returns(childFolders);

        _client.GetTestFolders(2)
            .Returns(new List<HPALMFolder>());

        _client.GetTestFolders(3)
            .Returns(new List<HPALMFolder>());

        var service = new SectionService(_logger, _client);

        // Act
        var result = await service.ConvertSections();

        // Assert
        Assert.That(result.Sections, Has.Count.EqualTo(1));
        Assert.That(result.Sections[0].Name, Is.EqualTo("Folder 1"));
        Assert.That(result.Sections[0].Sections, Has.Count.EqualTo(2));
        Assert.That(result.Sections[0].Sections[0].Name, Is.EqualTo("Subfolder 1"));
        Assert.That(result.Sections[0].Sections[0].Sections, Has.Count.EqualTo(0));
        Assert.That(result.Sections[0].Sections[1].Name, Is.EqualTo("Unattached"));
        Assert.That(result.Sections[0].Sections[1].Sections, Has.Count.EqualTo(0));
        Assert.That(result.SectionMap, Has.Count.EqualTo(3));
    }
}
