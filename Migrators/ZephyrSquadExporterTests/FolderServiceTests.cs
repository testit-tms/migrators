using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrSquadExporter.Client;
using ZephyrSquadExporter.Models;
using ZephyrSquadExporter.Services;

namespace ZephyrSquadExporterTests;

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
    public async Task GetSections_FailedGetCycles()
    {
        // Arrange
        _client.GetCycles()
            .Throws(new Exception("Failed to get cycles"));

        var service = new FolderService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.GetSections());

        // Assert
        await _client.DidNotReceive()
            .GetFolders(Arg.Any<string>());
    }

    [Test]
    public async Task GetSections_FailedGetFolders()
    {
        // Arrange
        var cycles = new List<ZephyrCycle>
        {
            new()
            {
                Id = "1",
                Name = "Cycle 1"
            }
        };

        _client.GetCycles()
            .Returns(cycles);

        _client.GetFolders(cycles[0].Id)
            .Throws(new Exception("Failed to get folders"));

        var service = new FolderService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.GetSections());
    }

    [Test]
    public async Task GetSections_Success()
    {
        // Arrange
        var cycles = new List<ZephyrCycle>
        {
            new()
            {
                Id = "1",
                Name = "Cycle 1"
            }
        };

        var folders = new List<ZephyrFolder>
        {
            new()
            {
                Id = "123",
                Name = "Folder 1"
            }
        };

        _client.GetCycles()
            .Returns(cycles);

        _client.GetFolders(cycles[0].Id)
            .Returns(folders);

        var service = new FolderService(_logger, _client);

        // Act
        var result =  await service.GetSections();

        // Assert
        Assert.That(result.Sections[0].Name, Is.EqualTo(cycles[0].Name));
        Assert.That(result.Sections[0].Sections[0].Name, Is.EqualTo(folders[0].Name));
        Assert.That(result.SectionMap, Has.Count.EqualTo(2));
    }
}
