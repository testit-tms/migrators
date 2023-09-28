using AzureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;
using Constants = AzureExporter.Models.Constants;

namespace AzureExporter.Services;

public class AttributeService : IAttributeService
{
    private readonly ILogger<AttributeService> _logger;
    private readonly IClient _client;

    public AttributeService(ILogger<AttributeService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<List<Attribute>> GetCustomAttributes(Guid projectId)
    {
        _logger.LogInformation("Getting custom attributes");

        var attributes = new List<Attribute>();

        var iterations = await _client.GetIterations(projectId);

        attributes.Add(new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.IterationAttributeName,
            Type = AttributeType.Options,
            IsActive = true,
            IsRequired = false,
            Options = iterations
        });

        attributes.Add(new Attribute
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
        });

        return attributes;
    }
}
