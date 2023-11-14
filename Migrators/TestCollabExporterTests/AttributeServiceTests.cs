using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TestCollabExporter.Client;
using TestCollabExporter.Models;
using TestCollabExporter.Services;

namespace TestCollabExporterTests;

public class AttributeServiceTests
{
    private ILogger<AttributeService> _logger;
    private IClient _client;
    private const int CompanyId = 1;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttributeService>>();
        _client = Substitute.For<IClient>();
    }

    [Test]
    public async Task ConvertAttributes_FailedGetCustomFields()
    {
        // Arrange
        _client.GetCustomFields(CompanyId)
            .Throws(new Exception("Test"));

        var service = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await service.ConvertAttributes(CompanyId));
    }

    [Test]
    public async Task ConvertAttributes_Success()
    {
        // Arrange
        var customFields = new List<TestCollabCustomField>
        {
            new()
            {
                Label = "Test",
                IsRequired = true,
                DefaultValue = "Test",
                Extra = new Extra
                {
                    Options = new List<Options>
                    {
                        new()
                        {
                            Value = "Test"
                        },
                        new()
                        {
                            Value = "Test2"
                        }
                    }
                }
            },
            new()
            {
                Label = "Test2",
                IsRequired = false,
                DefaultValue = "Test1",
                Extra = new Extra
                {
                    Options = new List<Options>()
                }
            }
        };

        _client.GetCustomFields(CompanyId)
            .Returns(customFields);

        var service = new AttributeService(_logger, _client);

        // Act
        var result = await service.ConvertAttributes(1);

        // Assert
        Assert.That(result.Attributes, Has.Count.EqualTo(2));
        Assert.That(result.Attributes[0].Name, Is.EqualTo(customFields[0].Label));
        Assert.That(result.Attributes[0].Type, Is.EqualTo(AttributeType.Options));
        Assert.That(result.Attributes[0].IsRequired, Is.EqualTo(customFields[0].IsRequired));
        Assert.That(result.Attributes[0].IsActive, Is.True);
        Assert.That(result.Attributes[0].Options, Has.Count.EqualTo(2));
        Assert.That(result.Attributes[0].Options[0], Is.EqualTo(customFields[0].Extra.Options[0].Value));
        Assert.That(result.Attributes[0].Options[1], Is.EqualTo(customFields[0].Extra.Options[1].Value));
        Assert.That(result.Attributes[1].Name, Is.EqualTo(customFields[1].Label));
        Assert.That(result.Attributes[1].Type, Is.EqualTo(AttributeType.String));
        Assert.That(result.Attributes[1].IsRequired, Is.EqualTo(customFields[1].IsRequired));
        Assert.That(result.Attributes[1].IsActive, Is.True);
        Assert.That(result.Attributes[1].Options, Is.Empty);
        Assert.That(result.AttributesMap, Has.Count.EqualTo(2));
    }
}
