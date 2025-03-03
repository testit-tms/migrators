using AllureExporter.Client;
using AllureExporter.Models.Project;
using AllureExporter.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;

namespace AllureExporterTests;

public class AttributeServiceTests
{
    private Mock<ILogger<AttributeService>> _logger;
    private Mock<IClient> _client;
    private AttributeService _sut;
    private List<BaseEntity> _customFields;
    private List<BaseEntity> _customFieldValues;
    private List<BaseEntity> _testLayers;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<AttributeService>>();
        _client = new Mock<IClient>();
        _sut = new AttributeService(_logger.Object, _client.Object);

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
    public void GetCustomAttributes_FailedGetCustomFieldNames()
    {
        // Arrange
        _client.Setup(x => x.GetCustomFieldNames(1))
            .ThrowsAsync(new Exception("Failed to get custom field names"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _sut.GetCustomAttributes(1));
        Assert.That(ex.Message, Is.EqualTo("Failed to get custom field names"));

        _client.Verify(x => x.GetCustomFieldValues(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        _client.Verify(x => x.GetTestLayers(), Times.Never);
    }

    [Test]
    public void GetCustomAttributes_FailedGetCustomFieldValues()
    {
        // Arrange
        _client.Setup(x => x.GetCustomFieldNames(1))
            .ReturnsAsync(_customFields);

        _client.Setup(x => x.GetCustomFieldValues(1, 1))
            .ThrowsAsync(new Exception("Failed to get custom field values"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _sut.GetCustomAttributes(1));
        Assert.That(ex.Message, Is.EqualTo("Failed to get custom field values"));

        _client.Verify(x => x.GetTestLayers(), Times.Never);
    }

    [Test]
    public void GetCustomAttributes_FailedGetTestLayers()
    {
        // Arrange
        _client.Setup(x => x.GetCustomFieldNames(1))
            .ReturnsAsync(_customFields);

        _client.Setup(x => x.GetCustomFieldValues(1, 1))
            .ReturnsAsync(_customFieldValues);

        _client.Setup(x => x.GetTestLayers())
            .ThrowsAsync(new Exception("Failed to get test layers"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _sut.GetCustomAttributes(1));
        Assert.That(ex.Message, Is.EqualTo("Failed to get test layers"));
    }

    [Test]
    public async Task GetCustomAttributes_Success()
    {
        // Arrange
        _client.Setup(x => x.GetCustomFieldNames(1))
            .ReturnsAsync(_customFields);

        _client.Setup(x => x.GetCustomFieldValues(1, 1))
            .ReturnsAsync(_customFieldValues);

        _client.Setup(x => x.GetTestLayers())
            .ReturnsAsync(_testLayers);

        // Act
        var attributes = await _sut.GetCustomAttributes(1);

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

        _client.Verify(x => x.GetCustomFieldNames(1), Times.Once);
        _client.Verify(x => x.GetCustomFieldValues(1, 1), Times.Once);
        _client.Verify(x => x.GetTestLayers(), Times.Once);
    }
}
