using Microsoft.Extensions.Logging;
using Models;
using TestRailExporter.Client;
using TestRailExporter.Models.Commons;
using Attribute = Models.Attribute;

namespace TestRailExporter.Services.Implementations;

public class AttributeService(ILogger<AttributeService> logger, IClient client) : IAttributeService
{
    public async Task<AttributeData> ConvertAttributes()
    {
        logger.LogInformation("Converting attributes");

        var fields = await client.GetCaseFields();
        var priorities = await client.GetPriorities();
        var statuses = await client.GetCaseStatuses();

        var data = new AttributeData
        {
            PriorityNames = priorities.ToDictionary(p => p.Id, p => p.Name),
            StatusNames = statuses.ToDictionary(s => s.Id, s => string.IsNullOrEmpty(s.Name) ? s.ShortName : s.Name)
        };

        foreach (var field in fields)
        {
            if (CaseConverter.IsSkippedField(field) || string.IsNullOrWhiteSpace(field.SystemName))
                continue;

            var attribute = new Attribute
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(field.Label) ? field.Name : field.Label,
                Type = CaseConverter.ConvertType(field.TypeId),
                IsActive = true,
                IsRequired = field.Configs.Any(c => c.Options?.IsRequired == true),
                Options = CaseConverter.ParseOptionLabels(field)
            };

            data.Attributes.Add(attribute);
            data.AttributesBySystemName[field.SystemName] = attribute;
            data.FieldsBySystemName[field.SystemName] = field;
        }

        logger.LogDebug("Converted {Count} attributes", data.Attributes.Count);
        return data;
    }
}
