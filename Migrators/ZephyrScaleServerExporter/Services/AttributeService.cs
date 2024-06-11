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

    public async Task<AttributeData> ConvertAttributes(string projectKey)
    {
        _logger.LogInformation("Converting attributes");

        var components = await _client.GetComponents(projectKey);

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
        };

        _logger.LogDebug("Attributes: {@Attribute}", attributes);

        return new AttributeData
        {
            Attributes = attributes,
            AttributeMap = attributes.ToDictionary(x => x.Name, x => x.Id),
        };
    }
}
