using AllureExporter.Client;
using AllureExporter.Models;
using AllureExporter.Services;
using AllureExporter.Services.Implementations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AllureExporterTests;

public class AttributeServiceTests
{
    private ILogger<AttributeService> _logger;
    private IClient _client;

    private List<BaseEntity> _customFields;
    private List<BaseEntity> _customFieldValues;
    private List<BaseEntity> _testLayers;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttributeService>>();
        _client = Substitute.For<IClient>();

        _customFields = new List<BaseEntity>
        {
            new()
            {
                Id = 1,
                Name = "Custom Field 1"
            }
        };

        _customFieldValues = new List<BaseEntity>
        {
            new()
            {
                Id = 1,
                Name = "Value 1"
            },
            new()
            {
                Id = 2,
                Name = "Value 2"
            },
            new()
            {
                Id = 3,
                Name = "Value 3"
            }
        };

        _testLayers = new List<BaseEntity>
        {
            new()
            {
                Id = 1,
                Name = "Test Layer 1"
            },
            new()
            {
                Id = 2,
                Name = "Test Layer 2"
            },
            new()
            {
                Id = 3,
                Name = "Test Layer 3"
            }
        };
    }

    [Test]
    public async Task GetCustomAttributes_FailedGetCustomFieldNames()
    {
        // Arrange
        _client.GetCustomFieldNames(1)
            .ThrowsAsync(new Exception("Failed to get custom field names"));

        var service = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.GetCustomAttributes(1));

        // Assert
        await _client.DidNotReceive()
            .GetCustomFieldValues(Arg.Any<int>(), Arg.Any<int>());

        await _client.DidNotReceive()
            .GetTestLayers();
    }

    [Test]
    public async Task GetCustomAttributes_FailedGetCustomFieldValues()
    {
        // Arrange
        _client.GetCustomFieldNames(1)
            .Returns(_customFields);

        _client.GetCustomFieldValues(1, 1)
            .ThrowsAsync(new Exception("Failed to get custom field values"));

        var service = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.GetCustomAttributes(1));

        // Assert
        await _client.DidNotReceive()
            .GetTestLayers();
    }

    [Test]
    public async Task GetCustomAttributes_FailedGetTestLayers()
    {
        // Arrange
        _client.GetCustomFieldNames(1)
            .Returns(_customFields);

        _client.GetCustomFieldValues(1, 1)
            .Returns(_customFieldValues);

        _client.GetTestLayers()
            .ThrowsAsync(new Exception("Failed to get test layers"));

        var service = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.GetCustomAttributes(1));
    }

    [Test]
    public async Task GetCustomAttributes_Success()
    {
        // Arrange
        _client.GetCustomFieldNames(1)
            .Returns(_customFields);

        _client.GetCustomFieldValues(1, 1)
            .Returns(_customFieldValues);

        _client.GetTestLayers()
            .Returns(_testLayers);

        var service = new AttributeService(_logger, _client);

        // Act
        var attributes = await service.GetCustomAttributes(1);

        // Assert
        Assert.That(attributes, Has.Count.EqualTo(3));
        Assert.That(attributes[0].Name, Is.EqualTo("Custom Field 1"));
        Assert.That(attributes[0].Options, Has.Count.EqualTo(3));
        Assert.That(attributes[0].Options[0], Is.EqualTo("Value 1"));
        Assert.That(attributes[0].Options[1], Is.EqualTo("Value 2"));
        Assert.That(attributes[0].Options[2], Is.EqualTo("Value 3"));
        Assert.That(attributes[1].Name, Is.EqualTo(Constants.AllureStatus));
        Assert.That(attributes[1].Options, Has.Count.EqualTo(4));
        Assert.That(attributes[1].Options[0], Is.EqualTo("Draft"));
        Assert.That(attributes[1].Options[1], Is.EqualTo("Active"));
        Assert.That(attributes[1].Options[2], Is.EqualTo("Outdated"));
        Assert.That(attributes[1].Options[3], Is.EqualTo("Review"));
        Assert.That(attributes[2].Name, Is.EqualTo(Constants.AllureTestLayer));
        Assert.That(attributes[2].Options, Has.Count.EqualTo(3));
        Assert.That(attributes[2].Options[0], Is.EqualTo("Test Layer 1"));
        Assert.That(attributes[2].Options[1], Is.EqualTo("Test Layer 2"));
        Assert.That(attributes[2].Options[2], Is.EqualTo("Test Layer 3"));
    }
}
