using AllureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;
using Constants = AllureExporter.Models.Constants;

namespace AllureExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<Attribute>> GetCustomAttributes(int projectId)
    {
        _logger.LogInformation("Getting custom attributes");

        var attributes = new List<Attribute>();

        var customFields = await _client.GetCustomFieldNames(projectId);

        foreach (var customField in customFields)
        {
            var values = await _client.GetCustomFieldValues(customField.Id);

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
            IsRequired = true,
            Type = AttributeType.Options,
            Options = new List<string>
            {
                "Draft",
                "Active",
                "Outdated",
                "Review"
            }
        });

        var testLayers = await _client.GetTestLayers();

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
