using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Attributes;
using ZephyrScaleServerExporter.Models.TestCases;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal class AttributeService(
    IDetailedLogService detailedLogService,
    ILogger<AttributeService> logger,
    IClient client)
    : IAttributeService
{
    public async Task<AttributeData> ConvertAttributes(string projectId)
    {
        logger.LogInformation("Converting attributes");

        var components = await client.GetComponents();
        var customFields = await client.GetCustomFieldsForTestCases(projectId);

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

        attributes.AddRange(ConvertCustomAttributes(customFields));

        detailedLogService.LogDebug("Attributes: {@Attribute}", attributes);

        return new AttributeData
        {
            Attributes = attributes,
            AttributeMap = attributes.ToDictionary(x => x.Name, x => x),
        };
    }

    public const string StateAttributeCloud = "Zephyr State";
    public const string PriorityAttributeCloud = "Zephyr Priority";

    public async Task<AttributeData> ConvertAttributesCloud(string projectKey)
    {
        logger.LogInformation("Converting attributes");

        var statuses = await client.GetStatusesCloud(projectKey);
        var priorities = await client.GetPrioritiesCloud(projectKey);
        // var customFields = await client.GetCustomFieldsForTestCases(projectId);


        var attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = StateAttributeCloud,
                Type = AttributeType.Options,
                IsRequired = false,
                IsActive = true,
                Options = statuses.Select(x => x.Name).ToList()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = PriorityAttributeCloud,
                IsRequired = false,
                IsActive = true,
                Type = AttributeType.Options,
                Options = priorities.Select(x => x.Name).ToList()
            }
        };

        logger.LogDebug("Attributes: {@Attribute}", attributes);


        return new AttributeData
        {
            Attributes = attributes,
            AttributeMap = attributes.ToDictionary(x => x.Name, x => x),
            // StateMap = statuses.ToDictionary(x => x.Id, x => x.Name),
            // PriorityMap = priorities.ToDictionary(x => x.Id, x => x.Name)
        };
    }

    private List<Attribute> ConvertCustomAttributes(List<ZephyrCustomFieldForTestCase> customFields)
    {
        var attributes = new List<Attribute>();

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
                if (customField.Options == null)
                {
                    detailedLogService.LogDebug("The attribute \"{Name}\" with {Type} type without options", customField.Name, customField.Type);

                    continue;
                }

                attribute.Options.AddRange(ConvertOptions(customField.Options));
            }

            attributes.Add(attribute);
        }

        return attributes;
    }

    private List<string> ConvertOptions(List<ZephyrCustomFieldOption> zephyrOptions)
    {
        var options = new List<string>();

        foreach (var option in zephyrOptions)
        {
            if (options.Contains(option.Name))
            {
                detailedLogService.LogDebug("The option \"{Option}\" has already been added to the attribute", option.Name);

                continue;
            }

            options.Add(option.Name);
        }

        return options;
    }

    private static AttributeType ConvertAttributeType(string zephyrAttributeType)
    {
        switch (zephyrAttributeType)
        {
            case ZephyrAttributeType.Options:
                return AttributeType.Options;
            case ZephyrAttributeType.MultipleOptions:
                return AttributeType.MultipleOptions;
            case ZephyrAttributeType.Datetime:
                return AttributeType.Datetime;
            case ZephyrAttributeType.Checkbox:
                return AttributeType.Checkbox;
            default:
                return AttributeType.String;
        }
    }
}
