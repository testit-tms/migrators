using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;
using Constants = AzureExporter.Models.Constants;

namespace AzureExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;

    public AttributeService(ILogger<AttributeService> logger)
    {
        _logger = logger;
    }

    public async Task<List<Attribute>> GetCustomAttributes(Guid projectId)
    {
        _logger.LogInformation("Getting custom attributes");

        var attributes = new List<Attribute>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.IterationAttributeName,
                Type = AttributeType.String,
                IsActive = true,
                IsRequired = false,
                Options = new List<string>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = Constants.StateAttributeName,
                Type = AttributeType.Options,
                IsActive = true,
                IsRequired = false,
                Options = new List<string>
                {
                    "Active",
                    "Closed",
                    "Design",
                    "Ready"
                }
            }
        };

        return attributes;
    }
}
