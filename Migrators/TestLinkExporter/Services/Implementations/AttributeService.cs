using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;
using Constants = TestLinkExporter.Models.Project.Constants;

namespace TestLinkExporter.Services.Implementations;

internal class AttributeService(ILogger<AttributeService> logger) : IAttributeService
{
    public List<Attribute> GetCustomAttributes()
    {
        logger.LogInformation("Getting custom attributes");

        var attributes = new List<Attribute>();

        attributes.Add(new Attribute
        {
            Id = Guid.NewGuid(),
            Name = Constants.TestLinkId,
            IsActive = true,
            IsRequired = false,
            Type = AttributeType.String,
        });

        return attributes;
    }
}
