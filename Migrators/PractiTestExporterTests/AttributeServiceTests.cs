using PractiTestExporter.Client;
using PractiTestExporter.Models;
using PractiTestExporter.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Models;

namespace PractiTestExporterTests;

public class AttributeServiceTests
{
    private ILogger<AttributeService> _logger;
    private IClient _client;

    private List<PractiTestCustomField> _customFields;
    private ListPractiTestCustomField _listCustomField;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttributeService>>();
        _client = Substitute.For<IClient>();

        _customFields = new List<PractiTestCustomField>
        {
            new()
            {
                Id = "123",
                Attributes = new CustomFieldAttributes
                {
                    Name = "test list",
                    FieldFormat = "list"
                }
            },
            new()
            {
                Id = "321",
                Attributes = new CustomFieldAttributes
                {
                    Name = "test text",
                    FieldFormat = "text"
                }
            },
            new()
            {
                Id = "111",
                Attributes = new CustomFieldAttributes
                {
                    Name = "test date",
                    FieldFormat = "date"
                }
            },
            new()
            {
                Id = "333",
                Attributes = new CustomFieldAttributes
                {
                    Name = "test any",
                    FieldFormat = "any"
                }
            }
        };

        _listCustomField = new ListPractiTestCustomField
        {
            Id = "123",
            Attributes = new ListCustomFieldAttributes
            {
                Name = "test",
                FieldFormat = "list",
                PossibleValues = new List<string> { "value1", "value2", "value3" }
            }
        };
    }

    [Test]
    public async Task GetCustomAttributes_FailedGetCustomFieldNames()
    {
        // Arrange
        _client.GetCustomFields()
            .ThrowsAsync(new Exception("Failed to get custom fields"));

        var service = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertCustomAttributes());

        // Assert
        await _client.DidNotReceive()
            .GetListCustomFieldById(Arg.Any<string>());
    }

    [Test]
    public async Task GetCustomAttributes_FailedGetCustomFieldValues()
    {
        // Arrange
        _client.GetCustomFields()
            .Returns(_customFields);

        _client.GetListCustomFieldById(_customFields[0].Id)
            .ThrowsAsync(new Exception("Failed to get custom field values"));

        var service = new AttributeService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertCustomAttributes());
    }

    [Test]
    public async Task GetCustomAttributes_Success()
    {
        // Arrange
        _client.GetCustomFields()
            .Returns(_customFields);

        _client.GetListCustomFieldById(_customFields[0].Id)
            .Returns(_listCustomField);

        var service = new AttributeService(_logger, _client);

        // Act
        var attributes = await service.ConvertCustomAttributes();

        // Assert
        Assert.That(attributes.Attributes, Has.Count.EqualTo(4));
        Assert.That(attributes.Attributes[0].Name, Is.EqualTo("test list"));
        Assert.That(attributes.Attributes[0].Type, Is.EqualTo(AttributeType.Options));
        Assert.That(attributes.Attributes[0].Options, Has.Count.EqualTo(3));
        Assert.That(attributes.Attributes[0].Options[0], Is.EqualTo("value1"));
        Assert.That(attributes.Attributes[0].Options[1], Is.EqualTo("value2"));
        Assert.That(attributes.Attributes[0].Options[2], Is.EqualTo("value3"));
        Assert.That(attributes.Attributes[1].Name, Is.EqualTo("test text"));
        Assert.That(attributes.Attributes[1].Type, Is.EqualTo(AttributeType.String));
        Assert.That(attributes.Attributes[1].Options, Has.Count.EqualTo(0));
        Assert.That(attributes.Attributes[2].Name, Is.EqualTo("test date"));
        Assert.That(attributes.Attributes[2].Type, Is.EqualTo(AttributeType.Datetime));
        Assert.That(attributes.Attributes[2].Options, Has.Count.EqualTo(0));
        Assert.That(attributes.Attributes[3].Name, Is.EqualTo("test any"));
        Assert.That(attributes.Attributes[3].Type, Is.EqualTo(AttributeType.String));
        Assert.That(attributes.Attributes[3].Options, Has.Count.EqualTo(0));
    }
}
