using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Constants;

namespace ZephyrScaleServerExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<AttributeData> ConvertAttributes(string projectId)
    {
        _logger.LogInformation("Converting attributes");

        var components = await _client.GetComponents();
        var customFields = await _client.GetCustomFieldsForTestCases(projectId);

        var attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.ComponentAttribute,
                Type = AttributeType.Options,
                IsRequired = false,
                IsActive = true,
                Options = components.Select(x => x.Name).ToList()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.IdZephyrAttribute,
                Type = AttributeType.String,
                IsRequired = false,
                IsActive = true,
                Options = new List<string>(),
            },
        };

        foreach (var customField in customFields)
        {
            var attribute = new Attribute()
            {
                Id = Guid.NewGuid(),
                Name = customField.Name,
                Type = ConvertAttributeType(customField.Type),
                IsRequired = customField.Required,
                IsActive = true,
                Options = new List<string>(),
            };

            if (attribute.Type == AttributeType.Options || attribute.Type == AttributeType.MultipleOptions)
            {
                attribute.Options.AddRange(customField.Options.Select(x => x.Name).ToList());
            }

            attributes.Add(attribute);
        }

        _logger.LogDebug("Attributes: {@Attribute}", attributes);

        return new AttributeData
        {
            Attributes = attributes,
            AttributeMap = attributes.ToDictionary(x => x.Name, x => x),
        };
    }

    private AttributeType ConvertAttributeType(string zephyrAttributeType)
    {
        switch (zephyrAttributeType)
        {
            case ZephyrAttributeType.Options:
                return AttributeType.Options;
            case ZephyrAttributeType.MultipleOptions:
                return AttributeType.MultipleOptions;
            case ZephyrAttributeType.Datetime:
                return AttributeType.Datetime;
            default:
                return AttributeType.String;
        }
    }
}
