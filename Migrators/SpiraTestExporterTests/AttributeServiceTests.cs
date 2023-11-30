using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpiraTestExporter.Client;
using SpiraTestExporter.Models;
using SpiraTestExporter.Services;

namespace SpiraTestExporterTests;

public class AttributeServiceTests
{
    private  ILogger<AttributeService> _logger;
    private  IClient _client;

    private const int ProjectTemplateId = 1;
    private List<SpiraPriority> _priorities;
    private List<SpiraStatus> _statuses;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttributeService>>();
        _client = Substitute.For<IClient>();

        _priorities = new List<SpiraPriority>
        {
            new()
            {
                Id = 1,
                Name = "High"
            },
            new()
            {
                Id = 2,
                Name = "Medium"
            },
            new()
            {
                Id = 3,
                Name = "Low"
            }
        };

        _statuses = new List<SpiraStatus>
        {
            new()
            {
                Id = 1,
                Name = "Open"
            },
            new()
            {
                Id = 2,
                Name = "In Progress"
            },
            new()
            {
                Id = 3,
                Name = "Closed"
            }
        };
    }

    [Test]
    public async Task GetAttributes_FailedGetPriorities()
    {
        // Arrange
        _client.GetPriorities(ProjectTemplateId)
            .Throws(new Exception("Failed to get priorities"));

        var attributeService = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await attributeService.GetAttributes(ProjectTemplateId));

        // Assert
        await _client.DidNotReceive()
            .GetStatuses(ProjectTemplateId);
    }

    [Test]
    public async Task GetAttributes_FailedGetStatuses()
    {
        // Arrange
        _client.GetPriorities(ProjectTemplateId)
            .Returns(_priorities);

        _client.GetStatuses(ProjectTemplateId)
            .Throws(new Exception("Failed to get statuses"));

        var attributeService = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async() => await attributeService.GetAttributes(ProjectTemplateId));
    }

    [Test]
    public async Task GetAttributes_Success()
    {
        // Arrange
        _client.GetPriorities(ProjectTemplateId)
            .Returns(_priorities);

        _client.GetStatuses(ProjectTemplateId)
            .Returns(_statuses);

        var attributeService = new AttributeService(_logger, _client);

        // Act
        var result = await attributeService.GetAttributes(ProjectTemplateId);

        // Assert
        Assert.That(result.Attributes, Has.Count.EqualTo(2));
        Assert.That(result.AttributesMap, Has.Count.EqualTo(2));
        Assert.That(result.PrioritiesMap, Has.Count.EqualTo(3));
        Assert.That(result.StatusesMap, Has.Count.EqualTo(3));
    }
}
