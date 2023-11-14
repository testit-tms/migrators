using Microsoft.Extensions.Logging;
using Models;
using PractiTestExporter.Client;
using PractiTestExporter.Models;
using Attribute = Models.Attribute;
using Constants = PractiTestExporter.Models.Constants;

namespace PractiTestExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<AttributeData> ConvertCustomAttributes()
    {
        _logger.LogInformation("Getting custom attributes");

        var attributes = new List<Attribute>();
        var attributeMap = new Dictionary<string, Guid>();

        var customFields = await _client.GetCustomFields();

        foreach (var customField in customFields)
        {
            var attribute = new Attribute
            {
                Id = Guid.NewGuid(),
                Name = customField.Attributes.Name,
                IsActive = true,
                IsRequired = false,
                Type = ConvertType(customField.Attributes.FieldFormat),
                Options = new List<string>()
            };

            if (attribute.Type == AttributeType.Options)
            {
                var listCustomField = await _client.GetListCustomFieldById(customField.Id);

                attribute.Options = listCustomField.Attributes.PossibleValues;
            }

            attributes.Add(attribute);
            attributeMap.Add(customField.Id, attribute.Id);
        }

        return new AttributeData
        {
            Attributes = attributes,
            AttributeMap = attributeMap,
        };
    }

    private static AttributeType ConvertType(string type)
    {
        return type switch
        {
            Constants.ListCustomFieldType or Constants.MultiListCustomFieldType => AttributeType.Options,
            Constants.DateCustomFieldType => AttributeType.Datetime,
            _ => AttributeType.String
        };
    }
}
