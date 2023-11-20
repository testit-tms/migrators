using Microsoft.Extensions.Logging;
using SpiraTestExporter.Client;
using SpiraTestExporter.Models;
using Attribute = Models.Attribute;

namespace SpiraTestExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<AttributeData> GetAttributes(int projectTemplateId)
    {
        _logger.LogInformation("Getting attributes for project template {ProjectTemplateId}", projectTemplateId);

        var priorities = await _client.GetPriorities(projectTemplateId);
        var statuses = await _client.GetStatuses(projectTemplateId);


        var attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.Priority,
                IsRequired = false,
                IsActive = true,
                Options = priorities.Select(p => p.Name).ToList()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.Status,
                IsRequired = false,
                IsActive = true,
                Options = statuses.Select(s => s.Name).ToList()
            }
        };

        return new AttributeData
        {
            Attributes = attributes,
            AttributesMap = attributes.ToDictionary(a => a.Name, a => a.Id),
            PrioritiesMap = priorities.ToDictionary(p => p.Id, p => p.Name),
            StatusesMap = statuses.ToDictionary(s => s.Id, s => s.Name)
        };
    }
}
