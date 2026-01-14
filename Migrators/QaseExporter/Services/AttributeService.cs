using Microsoft.Extensions.Logging;
using Models;
using QaseExporter.Client;
using QaseExporter.Models;
using System.Text.Json;
using Attribute = Models.Attribute;
using Constants = QaseExporter.Models.Constants;

namespace QaseExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;
    private List<string> _ignoredAttributeTitles = ["Description", "Pre-conditions", "Post-conditions", "Automation status (deprecated)", "Result status"];

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<AttributeData> ConvertAttributes()
    {
        _logger.LogInformation("Converting attributes");

        var attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.QaseIdAttribute,
                Type = AttributeType.String,
                IsRequired = false,
                IsActive = true,
                Options = new List<string>(),
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.QaseAuthorAttribute,
                Type = AttributeType.String,
                IsRequired = false,
                IsActive = true,
                Options = new List<string>(),
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.QaseCreatedDateAttribute,
                Type = AttributeType.Datetime,
                IsRequired = false,
                IsActive = true,
                Options = new List<string>(),
            },
        };

        var customAttributeMap = new Dictionary<QaseCustomField, Guid>();
        var systemAttributeMap = new Dictionary<QaseSystemField, Guid>();

        var customFields = await _client.GetCustomFields();
        var systemFields = await _client.GetSystemFields();

        foreach (var systemField in systemFields)
        {
            if (_ignoredAttributeTitles.Contains(systemField.Title))
            {
                continue;
            }

            var attribute = new Attribute()
            {
                Id = Guid.NewGuid(),
                Name = systemField.Title,
                Type = ConvertSystemAttributeType(systemField.Type),
                IsRequired = false,
                IsActive = true,
                Options = new List<string>(),
            };

            if (attribute.Type == AttributeType.Options)
            {
                attribute.Options.AddRange(systemField.Options.Select(x => x.Title).ToList());
            }

            attributes.Add(attribute);
            systemAttributeMap.Add(systemField, attribute.Id);
        }

        foreach (var customField in customFields)
        {
            var attribute = new Attribute()
            {
                Id = Guid.NewGuid(),
                Name = ConvertAttributeName(customField, attributes),
                Type = ConvertCustomAttributeType(customField.Type),
                IsRequired = false,
                IsActive = true,
                Options = new List<string>(),
            };

            if (attribute.Type == AttributeType.Options || attribute.Type == AttributeType.MultipleOptions)
            {
                customField.Options = JsonSerializer.Deserialize<List<QaseOption>>(customField.Value)!;

                if (customField.Options.Count == 0)
                {
                    _logger.LogError("Problems converting the value {Value} to options for the custom field {Name}", customField.Value, customField.Title);

                    continue;
                }

                attribute.Options.AddRange(customField.Options.Select(x => x.Title).ToList());
            }

            attributes.Add(attribute);
            customAttributeMap.Add(customField, attribute.Id);
        }

        _logger.LogDebug("Attributes: {@Attribute}", attributes);

        return new AttributeData
        {
            Attributes = attributes,
            CustomAttributeMap = customAttributeMap,
            SystemAttributeMap = systemAttributeMap,
            AttributeMap = attributes.ToDictionary(x => x.Name, x => x),
        };
    }

    private AttributeType ConvertCustomAttributeType(string qaseAttributeType)
    {
        switch (qaseAttributeType)
        {
            case QaseAttributeType.Options:
                return AttributeType.Options;
            case QaseAttributeType.MultipleOptions:
                return AttributeType.MultipleOptions;
            case QaseAttributeType.Datetime:
                return AttributeType.Datetime;
            case QaseAttributeType.Checkbox:
                return AttributeType.Checkbox;
            default:
                return AttributeType.String;
        }
    }

    private AttributeType ConvertSystemAttributeType(int qaseSysAttributeType)
    {
        switch (qaseSysAttributeType)
        {
            case QaseSysAttributeType.Options:
                return AttributeType.Options;
            case QaseSysAttributeType.Checkbox:
                return AttributeType.Checkbox;
            default:
                return AttributeType.String;
        }
    }

    private static string ConvertAttributeName(QaseCustomField attribute, List<Attribute> attributes)
    {
        var newName = attribute.Title;
        var i = 1;

        while (attributes.Any(x => x.Name == newName))
        {
            newName = $"{attribute.Title} ({i})";
            i++;
        }

        return newName;
    }
}
