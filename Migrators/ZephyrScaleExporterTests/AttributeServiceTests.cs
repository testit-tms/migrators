using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;
using ZephyrScaleExporter.Services;
using Constants = ZephyrScaleExporter.Models.Constants;

namespace ZephyrScaleExporterTests;

public class AttributeServiceTests
{
    private ILogger<AttributeService> _logger;
    private IClient _client;
    private List<ZephyrStatus> _statuses;
    private List<ZephyrPriority> _priorities;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttributeService>>();
        _client = Substitute.For<IClient>();

        _statuses = new List<ZephyrStatus>
        {
            new()
            {
                Id = 123,
                Name = "To Do"
            },
            new()
            {
                Id = 124,
                Name = "In Progress"
            },
            new()
            {
                Id = 125,
                Name = "Done"
            }
        };

        _priorities = new List<ZephyrPriority>
        {
            new()
            {
                Id = 321,
                Name = "Low"
            },
            new()
            {
                Id = 432,
                Name = "Medium"
            },
            new()
            {
                Id = 543,
                Name = "High"
            }
        };
    }

    [Test]
    public async Task ConvertAttributes_Success()
    {
        // Arrange
        _client.GetStatuses()
            .Returns(_statuses);

        _client.GetPriorities()
            .Returns(_priorities);

        var service = new AttributeService(_logger, _client);

        // Act
        var result = await service.ConvertAttributes();

        // Assert
        Assert.That(result.Attributes, Has.Count.EqualTo(2));
        Assert.That(result.Attributes[0].Name, Is.EqualTo(Constants.StateAttribute));
        Assert.That(result.Attributes[0].Type, Is.EqualTo(AttributeType.Options));
        Assert.That(result.Attributes[0].Options, Has.Count.EqualTo(3));
        Assert.That(result.Attributes[0].Options[0], Is.EqualTo("To Do"));
        Assert.That(result.Attributes[0].Options[1], Is.EqualTo("In Progress"));
        Assert.That(result.Attributes[0].Options[2], Is.EqualTo("Done"));
        Assert.That(result.Attributes[1].Name, Is.EqualTo(Constants.PriorityAttribute));
        Assert.That(result.Attributes[1].Type, Is.EqualTo(AttributeType.Options));
        Assert.That(result.Attributes[1].Options, Has.Count.EqualTo(3));
        Assert.That(result.Attributes[1].Options[0], Is.EqualTo("Low"));
        Assert.That(result.Attributes[1].Options[1], Is.EqualTo("Medium"));
        Assert.That(result.Attributes[1].Options[2], Is.EqualTo("High"));
        Assert.That(result.AttributeMap, Has.Count.EqualTo(2));
        Assert.That(result.StateMap, Has.Count.EqualTo(3));
        Assert.That(result.PriorityMap, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task ConvertAttributes_FailedGetStatuses()
    {
        // Arrange
        _client.GetStatuses()
            .Throws(new Exception("Failed to get statuses"));

        var service = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertAttributes());

        // Assert
        await _client.DidNotReceive()
            .GetPriorities();
    }

    [Test]
    public async Task ConvertAttributes_FailedGetPriorities()
    {
        // Arrange
        _client.GetStatuses()
            .Returns(_statuses);

        _client.GetPriorities()
            .Throws(new Exception("Failed to get priorities"));

        var service = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertAttributes());
    }
}
