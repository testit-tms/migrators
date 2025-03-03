using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Attribute = Models.Attribute;

namespace Importer.Services.Implementations;

internal class AttributeService(ILogger<AttributeService> logger, IClientAdapter clientAdapter)
    : IAttributeService
{
    public async Task<Dictionary<Guid, TmsAttribute>> ImportAttributes(Guid projectId,
        IEnumerable<Attribute> attributes)
    {
        logger.LogInformation("Importing attributes");

        var projectAttributes = await clientAdapter.GetProjectAttributes();
        var unusedRequiredProjectAttributes = await clientAdapter.GetRequiredProjectAttributesByProjectId(projectId);

        var attributesMap = new Dictionary<Guid, TmsAttribute>();

        foreach (var attribute in attributes)
        {
            var requiredProjectAttribute =
                unusedRequiredProjectAttributes.FirstOrDefault(x => x.Name == attribute.Name);

            if (requiredProjectAttribute != null && requiredProjectAttribute.Type == attribute.Type.ToString())
                unusedRequiredProjectAttributes.Remove(requiredProjectAttribute);

            var attributeIsNotImported = true;

            do
            {
                var projectAttribute = projectAttributes.FirstOrDefault(x => x.Name == attribute.Name);

                if (projectAttribute == null)
                {
                    logger.LogInformation("Creating attribute {Name}", attribute.Name);

                    var attributeId = await clientAdapter.ImportAttribute(attribute);
                    attributeId = await clientAdapter.GetAttribute(attributeId.Id);
                    attributesMap.Add(attribute.Id, attributeId);
                    attributeIsNotImported = false;
                }
                else
                {
                    if (projectAttribute.Type == attribute.Type.ToString())
                    {
                        logger.LogInformation("Attribute {Name} already exists with id {Id}",
                            attribute.Name,
                            projectAttribute.Id);

                        if (string.Equals(projectAttribute.Type, "options",
                                StringComparison.InvariantCultureIgnoreCase) ||
                            string.Equals(projectAttribute.Type, "multipleOptions",
                                StringComparison.InvariantCultureIgnoreCase))
                        {
                            var options = projectAttribute.Options.Select(o => o.Value).ToList();

                            foreach (var option in attribute.Options)
                                if (!string.IsNullOrEmpty(option) && !options.Contains(option))
                                    projectAttribute.Options.Add(new TmsAttributeOptions
                                    {
                                        Value = option,
                                        IsDefault = false
                                    });

                            await clientAdapter.UpdateAttribute(projectAttribute);
                            projectAttribute = await clientAdapter.GetProjectAttributeById(projectAttribute.Id);
                        }

                        attributesMap.Add(attribute.Id, projectAttribute);

                        attributeIsNotImported = false;
                    }
                    else
                    {
                        var newName = GetNewAttributeName(attribute, projectAttributes);
                        attribute.Name = newName;
                    }
                }
            } while (attributeIsNotImported);
        }

        foreach (var unusedRequiredProjectAttribute in unusedRequiredProjectAttributes)
        {
            logger.LogInformation(
                "Required project attribute {Name} is not used when importing test cases. Set as optional",
                unusedRequiredProjectAttribute.Name);

            unusedRequiredProjectAttribute.IsRequired = false;

            await clientAdapter.UpdateProjectAttribute(projectId, unusedRequiredProjectAttribute);
        }

        logger.LogInformation("Importing attributes finished");
        logger.LogDebug("Attributes map: {@AttributesMap}", attributesMap);

        if (attributesMap.Count > 0)
            await clientAdapter.AddAttributesToProject(projectId, attributesMap.Values.Select(x => x.Id));

        return attributesMap;
    }

    private static string GetNewAttributeName(Attribute attribute, IEnumerable<TmsAttribute> attributes)
    {
        var newName = attribute.Name;

        var i = 1;

        var tmsAttributes = attributes.ToList();
        while (tmsAttributes.Any(x => x.Name == newName && x.Type != attribute.Type.ToString()))
        {
            newName = $"{attribute.Name} ({i})";
            i++;
        }

        return newName;
    }
}