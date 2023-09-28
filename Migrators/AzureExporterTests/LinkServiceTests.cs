using AzureExporter.Models;
using AzureExporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AzureExporterTests;

public class LinkServiceTests
{
    private ILogger<LinkService> _logger;
    private IConfiguration _configuration;
    private IConfigurationSection _configurationSection;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<LinkService>>();
    }

    [Test]
    public void Constructor_UrlNotSpecified()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string> {
            {"azure:url", ""},
            {"azure:projectName", ""}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Act
        Assert.Throws<ArgumentException>(() => new LinkService(_logger, _configuration));
    }

    [Test]
    public void Constructor_ProjectNotSpecified()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string> {
            {"azure:url", "https://dev.azure.com/test"},
            {"azure:projectName", ""}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Act
        Assert.Throws<ArgumentException>(() => new LinkService(_logger, _configuration));
    }

    [Test]
    public void Constructor_UrlAndProjectSpecified()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string> {
            {"azure:url", "https://dev.azure.com/test"},
            {"azure:projectName", "test"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Act
        var linkService = new LinkService(_logger, _configuration);

        // Assert
        Assert.That(linkService, Is.Not.Null);
    }

    [Test]
    public void ConvertLinks()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string> {
            {"azure:url", "https://dev.azure.com/organization"},
            {"azure:projectName", "projectName"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var linkService = new LinkService(_logger, _configuration);

        var azureLinks = new List<AzureLink>
        {
            new()
            {
                Title = "Branch",
                Url = "vstfs:///Git/Ref/9ddad9fc-a9f5-4bb6-86ed-afdd158effb3%2F042b81d6-d793-4bb5-99ef-57f2fac0e194%2FGBmain"
            },
            new()
            {
                Title = "Commit",
                Url = "vstfs:///Git/Commit/9ddad9fc-a9f5-4bb6-86ed-afdd158effb3%2F042b81d6-d793-4bb5-99ef-57f2fac0e194%2Fff293568f8de16bac45a4c19918f8e52b550c1c1"
            }
        };

        // Act
        var links = linkService.CovertLinks(azureLinks);

        // Assert
        Assert.That(links, Is.Not.Null);
        Assert.That(links, Has.Count.EqualTo(2));
        Assert.That(links[0].Url, Is.EqualTo("https://dev.azure.com/organization/projectName/_git/042b81d6-d793-4bb5-99ef-57f2fac0e194?version=GBmain"));
        Assert.That(links[0].Title, Is.EqualTo("Branch"));
        Assert.That(links[1].Url, Is.EqualTo("https://dev.azure.com/organization/projectName/_git/042b81d6-d793-4bb5-99ef-57f2fac0e194/commit/ff293568f8de16bac45a4c19918f8e52b550c1c1"));
        Assert.That(links[1].Title, Is.EqualTo("Commit"));
    }
}
