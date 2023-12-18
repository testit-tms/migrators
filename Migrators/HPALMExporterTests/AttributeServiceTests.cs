using HPALMExporter.Client;
using HPALMExporter.Models;
using HPALMExporter.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HPALMExporterTests;

public class AttributeServiceTests
{
    private ILogger<AttributeService> _logger;
    private IClient _client;

    private HPALMAttributes _hpalmAttributes;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttributeService>>();
        _client = Substitute.For<IClient>();

        _hpalmAttributes = new HPALMAttributes
        {
            Fields = new HPALMFields
            {
                Field = new List<HPALMField>
                {
                    new()
                    {
                        Required = true,
                        System = false,
                        Type = "LookupList",
                        Active = true,
                        Name = "TestAttribute",
                        Label = "TestAttribute",
                        PhysicalName = "TestAttribute",
                        ListId = 1
                    },
                    new()
                    {
                        Required = true,
                        System = false,
                        Type = "String",
                        Active = true,
                        Name = "TestAttribute2",
                        Label = "TestAttribute2",
                        PhysicalName = "TestAttribute2",
                        ListId = null
                    },
                    new()
                    {
                        Required = true,
                        System = true,
                        Type = "String",
                        Active = true,
                        Name = "TestAttribute3",
                        Label = "TestAttribute3",
                        PhysicalName = "TestAttribute3",
                        ListId = null
                    }
                }
            }
        };
    }

    [Test]
    public async Task ConvertAttributes_FailedGetTestAttributes()
    {
        // Arrange
        _client.GetTestAttributes()
            .Throws(new Exception("Failed to get test attributes"));

        var attributeService = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await attributeService.ConvertAttributes());

        // Assert
        await _client.DidNotReceive().GetLists();
    }

    [Test]
    public async Task ConvertAttributes_FailedGetLists()
    {
        // Arrange
        _client.GetTestAttributes()
            .Returns(_hpalmAttributes);

        _client.GetLists()
            .Throws(new Exception("Failed to get lists"));

        var attributeService = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await attributeService.ConvertAttributes());
    }

    [Test]
    public async Task ConvertAttributes()
    {
        // Arrange
        var hpalmLists = new HPALMLists
        {
            Root = new List<HPALMList>
            {
                new()
                {
                    Id = 1,
                    Items = new List<HPALMItem>
                    {
                        new()
                        {
                            Value = "Value1"
                        },
                        new()
                        {
                            Value = "Value2"
                        }
                    }
                }
            }
        };

        _client.GetTestAttributes()
            .Returns(_hpalmAttributes);

        _client.GetLists()
            .Returns(hpalmLists);

        var attributeService = new AttributeService(_logger, _client);

        // Act
        var attributes = await attributeService.ConvertAttributes();

        // Assert
        Assert.That(attributes, Has.Count.EqualTo(2));
        Assert.That(attributes[0].Name, Is.EqualTo("TestAttribute"));
        Assert.That(attributes[0].Type, Is.EqualTo(AttributeType.Options));
        Assert.That(attributes[0].Options, Has.Count.EqualTo(2));
        Assert.That(attributes[0].Options[0], Is.EqualTo("Value1"));
        Assert.That(attributes[0].Options[1], Is.EqualTo("Value2"));
        Assert.That(attributes[1].Name, Is.EqualTo("TestAttribute2"));
        Assert.That(attributes[1].Type, Is.EqualTo(AttributeType.String));
        Assert.That(attributes[1].Options, Is.Empty);
    }
}
