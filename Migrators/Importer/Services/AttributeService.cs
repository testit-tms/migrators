using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Attribute = Models.Attribute;

namespace Importer.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<Dictionary<Guid, TmsAttribute>> ImportAttributes(IEnumerable<Attribute> attributes)
    {
        _logger.LogInformation("Importing attributes");

        var projectAttributes = await _client.GetProjectAttributes();

        var attributesMap = new Dictionary<Guid, TmsAttribute>();

        foreach (var attribute in attributes)
        {
            var projectAttribute = projectAttributes.FirstOrDefault(x => x.Name == attribute.Name);

            if (projectAttribute == null)
            {
                _logger.LogInformation("Creating attribute {Name}", attribute.Name);

                var attributeId = await _client.ImportAttribute(attribute);

                attributesMap.Add(attribute.Id, attributeId);
            }
            else
            {
                // todo: add update logic
                if (projectAttribute.Type == attribute.Type.ToString())
                {
                    _logger.LogInformation("Attribute {Name} already exists with id {Id}",
                        attribute.Name,
                        attribute.Id);

                    attributesMap.Add(attribute.Id, projectAttribute);
                }
                else
                {
                    var newName = GetNewAttributeName(attribute, projectAttributes);
                    attribute.Name = newName;

                    _logger.LogInformation("Creating attribute {Name}", attribute.Name);

                    var attributeId = await _client.ImportAttribute(attribute);

                    attributesMap.Add(attribute.Id, attributeId);
                }
            }
        }

        _logger.LogInformation("Importing attributes finished");
        _logger.LogDebug("Attributes map: {@AttributesMap}", attributesMap);

        if (attributesMap.Count > 0)
        {
            await _client.AddAttributesToProject(attributesMap.Values.Select(x => x.Id));
        }

        return attributesMap;
    }

    private static string GetNewAttributeName(Attribute attribute, IEnumerable<TmsAttribute> attributes)
    {
        var newName = attribute.Name;

        var i = 1;

        var tmsAttributes = attributes.ToList();
        while (tmsAttributes.Any(x => x.Name == newName && x.Type == attribute.Type.ToString()))
        {
            newName = $"{attribute.Name} ({i})";
            i++;
        }

        return newName;
    }
}
