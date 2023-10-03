using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleExporter.Client;
using ZephyrScaleExporter.Models;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleExporter.Models.Constants;

namespace ZephyrScaleExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<AttributeData> ConvertAttributes()
    {
        _logger.LogInformation("Converting attributes");

        var statuses = await _client.GetStatuses();
        var priorities = await _client.GetPriorities();

        var attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.StateAttribute,
                Type = AttributeType.Options,
                IsRequired = false,
                IsActive = true,
                Options = statuses.Select(x => x.Name).ToList()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.PriorityAttribute,
                IsRequired = false,
                IsActive = true,
                Type = AttributeType.Options,
                Options = priorities.Select(x => x.Name).ToList()
            }
        };

        _logger.LogDebug("Attributes: {@Attribute}", attributes);

        return new AttributeData
        {
            Attributes = attributes,
            AttributeMap = attributes.ToDictionary(x => x.Name, x => x.Id),
            StateMap = statuses.ToDictionary(x => x.Id, x => x.Name),
            PriorityMap = priorities.ToDictionary(x => x.Id, x => x.Name)
        };
    }
}
