using Importer.Client;
using Importer.Models;
using Importer.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Attribute = Models.Attribute;

namespace ImporterTests;

public class AttributeServiceTests
{
    private ILogger<AttributeService> _logger;
    private IClient _client;
    private List<Attribute> _attributes;
    private Dictionary<Guid, TmsAttribute> _attributesMap;
    private List<TmsAttribute> _tmsAttributes;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttributeService>>();
        _client = Substitute.For<IClient>();
        _attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee6"),
                Name = "TestAttribute",
                IsActive = true,
                IsRequired = false,
                Type = AttributeType.String,
                Options = new List<string>()
            },
            new()
            {
                Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad0d"),
                Name = "TestAttribute2",
                IsActive = true,
                IsRequired = false,
                Type = AttributeType.Options,
                Options = new List<string> { "Option1", "Option2" }
            }
        };
        _tmsAttributes = new List<TmsAttribute>()
        {
            new()
            {
                Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbe10"),
                Name = "TestAttribute",
                IsRequired = false,
                IsEnabled = true,
                Type = "String",
                Options = new List<TmsAttributeOptions>()
            },
            new()
            {
                Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad10"),
                Name = "TestAttribute2",
                IsRequired = false,
                IsEnabled = true,
                Type = "Options",
                Options = new List<TmsAttributeOptions>
                {
                    new()
                    {
                        Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad11"),
                        Value = "Option1",
                        IsDefault = true
                    },
                    new()
                    {
                        Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad12"),
                        Value = "Option2",
                        IsDefault = false
                    }
                }
            }
        };
        _attributesMap = new Dictionary<Guid, TmsAttribute>
        {
            {
                Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee6"),
                _tmsAttributes[0]
            },
            {
                Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad0d"),
                _tmsAttributes[1]
            }
        };
    }

    [Test]
    public async Task ImportAttributes_FailedGetProjectAttributes()
    {
        // Arrange
        _client.GetProjectAttributes().ThrowsAsync(new Exception("Failed to get project attributes"));

        var attributeService = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await attributeService.ImportAttributes(_attributes));

        // Assert
        await _client.DidNotReceive().ImportAttribute(Arg.Any<Attribute>());
        await _client.DidNotReceive().UpdateAttribute(Arg.Any<TmsAttribute>());
    }

    [Test]
    public async Task ImportAttributes_FailedImportAttribute()
    {
        // Arrange
        _client.GetProjectAttributes().Returns(new List<TmsAttribute>());
        _client.ImportAttribute(_attributes[0]).ThrowsAsync(new Exception("Failed to import attribute"));
        var attributeService = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await attributeService.ImportAttributes(_attributes));

        // Assert
        await _client.Received().ImportAttribute(_attributes[0]);
        await _client.DidNotReceive().ImportAttribute(_attributes[1]);
        await _client.DidNotReceive().UpdateAttribute(Arg.Any<TmsAttribute>());
    }

    [Test]
    public async Task ImportAttributes_FailedUpdateAttribute()
    {
        // Arrange
        _client.GetProjectAttributes().Returns(new List<TmsAttribute> { _tmsAttributes[1] });
        _client.UpdateAttribute(_tmsAttributes[1]).ThrowsAsync(new Exception("Failed to update attribute"));
        var attributeService = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await attributeService.ImportAttributes(new[] { _attributes[1] }));

        // Assert
        await _client.DidNotReceive().ImportAttribute(Arg.Any<Attribute>());
    }

    [Test]
    public async Task ImportAttributes_ImportAttributeSuccess()
    {
        // Arrange
        _client.GetProjectAttributes().Returns(new List<TmsAttribute>());
        _client.ImportAttribute(_attributes[1]).Returns(_attributesMap[_attributes[1].Id]);
        _client.ImportAttribute(_attributes[0]).Returns(_attributesMap[_attributes[0].Id]);
        var attributeService = new AttributeService(_logger, _client);

        // Act
        var resp = await attributeService.ImportAttributes(_attributes);

        // Assert
        await _client.DidNotReceive().UpdateAttribute(Arg.Any<TmsAttribute>());
        Assert.That(resp, Is.EqualTo(_attributesMap));
    }

    [Test]
    public async Task ImportAttributes_UpdateAttributeSuccess()
    {
        // Arrange
        _client.GetProjectAttributes().Returns(new List<TmsAttribute> { _tmsAttributes[1] });
        _client.UpdateAttribute(_tmsAttributes[1]).Returns(_tmsAttributes[1]);
        var attributeService = new AttributeService(_logger, _client);

        // Act
        var resp = await attributeService.ImportAttributes(new[] { _attributes[1] });

        // Assert
        await _client.DidNotReceive().ImportAttribute(Arg.Any<Attribute>());
        Assert.That(resp, Is.EqualTo(new Dictionary<Guid, TmsAttribute>()
        {
            { _attributes[1].Id, _attributesMap[_attributes[1].Id] }
        }));
    }

    [Test]
    public async Task ImportAttributes_ImportAttributeWithNewNameSuccess()
    {
        // Arrange
        var respAttributes = new List<TmsAttribute>
        {
            _tmsAttributes[0]
        };
        respAttributes[0].Type = "data";

        _client.GetProjectAttributes().Returns(respAttributes);
        var newAttribute = _attributes[0];
        newAttribute.Name = "TestAttribute (1)";
        var respAttribute = _tmsAttributes[0];
        respAttribute.Name = "TestAttribute (1)";
        _client.ImportAttribute(newAttribute).Returns(respAttribute);
        var attributeService = new AttributeService(_logger, _client);
        var map = new Dictionary<Guid, TmsAttribute> { { _attributes[0].Id, respAttribute } };

        // Act
        var resp = await attributeService.ImportAttributes(new[] { _attributes[0] });

        // Assert
        await _client.DidNotReceive().UpdateAttribute(Arg.Any<TmsAttribute>());
        Assert.That(resp, Is.EqualTo(map));
    }
}
