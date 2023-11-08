using Microsoft.Extensions.Logging;
using Models;
using TestCollabExporter.Client;
using TestCollabExporter.Models;
using Attribute = Models.Attribute;

namespace TestCollabExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<AttributeData> ConvertAttributes(int companyId)
    {
        _logger.LogInformation("Converting attributes");

        var customFields = await _client.GetCustomFields(companyId);

        var attributes = new List<Attribute>();
        var attributesMap = new Dictionary<string, Guid>();

        foreach (var customField in customFields)
        {
            var attribute = new Attribute
            {
                Id = Guid.NewGuid(),
                Name = customField.Label,
                Type = customField.Extra.Options.Count > 0 ? AttributeType.Options : AttributeType.String,
                IsRequired = customField.IsRequired,
                IsActive = true,
                Options = customField.Extra.Options.Select(x => x.Value).ToList()
            };

            attributes.Add(attribute);
            attributesMap.Add(customField.Label, attribute.Id);
        }

        return new AttributeData
        {
            Attributes = attributes,
            AttributesMap = attributesMap
        };
    }
}
