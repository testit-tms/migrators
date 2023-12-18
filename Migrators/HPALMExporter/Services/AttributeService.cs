using HPALMExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;

namespace HPALMExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<Attribute>> ConvertAttributes()
    {
        _logger.LogInformation("Convert attributes from HP ALM");

        var hpalmAttributes = await _client.GetTestAttributes();
        var valuesOfAttributes = await _client.GetLists();

        var attributes = new List<Attribute>();

        foreach (var hpalmAttribute in hpalmAttributes.Fields.Field.Where(a => !a.System))
        {
            var attribute = new Attribute
            {
                Id = Guid.NewGuid(),
                Name = hpalmAttribute.Label,
                IsActive = hpalmAttribute.Active,
                IsRequired = hpalmAttribute.Required,
                Type = AttributeType.String,
                Options = new List<string>()
            };

            if (hpalmAttribute.Type == "LookupList")
            {
                attribute.Type = AttributeType.Options;

                var values = valuesOfAttributes.Root.FirstOrDefault(a => a.Id == hpalmAttribute.ListId);
                if (values != null)
                {
                    attribute.Options = values.Items.Select(v => v.Value).ToList();
                }
            }

            attributes.Add(attribute);
        }

        return attributes;
    }
}
