using AllureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;
using Constants = AllureExporter.Models.Project.Constants;

namespace AllureExporter.Services.Implementations;

internal class AttributeService(ILogger<AttributeService> logger, IClient client) : IAttributeService
{
    public async Task<List<Attribute>> GetCustomAttributes(long projectId)
    {
        logger.LogInformation("Getting custom attributes");

        var attributes = new List<Attribute>();

        var customFields = await client.GetCustomFieldNames(projectId);

        foreach (var customField in customFields)
        {
            var values = await client.GetCustomFieldValues(customField.Id, projectId);

            var attribute = new Attribute
            {
                Id = Guid.NewGuid(),
                Name = customField.Name,
                IsActive = true,
                IsRequired = false,
                Type = AttributeType.Options,
                Options = values.Select(v => v.Name).ToList()
            };

            attributes.Add(attribute);
        }

        attributes.Add(new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.AllureStatus,
            IsActive = true,
            IsRequired = false,
            Type = AttributeType.Options,
            Options = new List<string>
            {
                "Draft",
                "Active",
                "Outdated",
                "Review"
            }
        });

        var testLayers = await client.GetTestLayers();

        attributes.Add(new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.AllureTestLayer,
            IsActive = true,
            IsRequired = false,
            Type = AttributeType.Options,
            Options = testLayers.Select(l => l.Name).ToList()
        });

        return attributes;
    }
}
