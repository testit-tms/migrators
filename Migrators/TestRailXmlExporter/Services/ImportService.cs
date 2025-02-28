using System.Xml.Linq;
using System.Xml.Serialization;
using TestRailXmlExporter.Enums;
using TestRailXmlExporter.Models;

namespace TestRailXmlExporter.Services;

public class ImportService
{
    private readonly XmlSerializer _xmlSerializer;

    public ImportService(XmlSerializer xmlSerializer)
    {
        _xmlSerializer = xmlSerializer;
    }

    public async Task<(TestRailsXmlSuite testRailsXmlSuite, List<CustomAttributeModel> customAttributes)> ImportXmlAsync(
        string? filePath)
    {
        await using var fileStream = File.OpenRead(filePath!);
        await using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

        memoryStream.Seek(0, SeekOrigin.Begin);
        var testRailsXmlSuite = (TestRailsXmlSuite)_xmlSerializer.Deserialize(memoryStream)!;

        memoryStream.Seek(0, SeekOrigin.Begin);
        var customAttributes = await GetCustomAttributesAsync(memoryStream).ConfigureAwait(false);

        return (testRailsXmlSuite, customAttributes);
    }

    private static async Task<List<CustomAttributeModel>> GetCustomAttributesAsync(Stream fileStream)
    {
        var knownAttribute = new[]
        {
            "comment",
            "preconds",
            "steps_separated",
            "steps",
            "expected",
            "mission",
            "goals",
            "estimate"
        };

        var attributesScheme = new List<CustomAttributeModel>();

        var xml = await XDocument.LoadAsync(fileStream, LoadOptions.PreserveWhitespace, default).ConfigureAwait(false);

        var customAttributesOfTestCases = xml.Descendants("custom")
            .SelectMany(xElement => xElement.Elements())
            .Where(xElement => knownAttribute.Contains(xElement.Name.LocalName) == false)
            .GroupBy(xElement => xElement.Name.LocalName)
            .ToList();

        foreach (var attributeGroup in customAttributesOfTestCases)
        {
            var attributeType = GetAttributeType(attributeGroup);

            var attributeModel = new CustomAttributeModel
            {
                IsEnabled = true,
                Name = attributeGroup.Key,
                IsRequired = false,
                Type = attributeType,
            };

            switch (attributeType)
            {
                case CustomAttributeTypesEnum.MultipleOptions:
                    {
                        attributeModel.IsGlobal = true;

                        var values = attributeGroup
                            .SelectMany(xElement => xElement.Elements("item"))
                            .OrderBy(xElement => long.TryParse(xElement.Element("id")?.Value, out var value) ? value : 0L)
                            .Select(xElement => xElement.Element("value")?.Value)
                            .Distinct()
                            .ToList();

                        attributeModel.Options = new List<CustomAttributeOptionModel>(values.Select(value =>
                            new CustomAttributeOptionModel
                            {
                                Value = value
                            }));

                        break;
                    }

                case CustomAttributeTypesEnum.Options:
                    {
                        var values = attributeGroup
                            .OrderBy(xElement => long.TryParse(xElement.Element("id")?.Value, out var value) ? value : 0L)
                            .Select(xElement => xElement.Element("value")?.Value)
                            .Distinct()
                            .ToList();

                        attributeModel.Options = new List<CustomAttributeOptionModel>(values.Select(value =>
                            new CustomAttributeOptionModel
                            {
                                Value = value
                            }));

                        break;
                    }

                case CustomAttributeTypesEnum.CheckBox:
                    {
                        attributeModel.IsGlobal = true;
                        break;
                    }
            }

            attributesScheme.Add(attributeModel);
        }

        var existReferences = xml.Descendants(nameof(TestRailsXmlCase.References).ToLower()).Any();

        if (existReferences)
        {
            attributesScheme.Add(new CustomAttributeModel
            {
                IsEnabled = true,
                Name = nameof(TestRailsXmlCase.References),
                IsRequired = false,
                Type = CustomAttributeTypesEnum.String
            });
        }

        var typeElements = xml.Descendants(nameof(TestRailsXmlCase.Type).ToLower());

        if (typeElements.Any())
        {
            attributesScheme.Add(new CustomAttributeModel
            {
                IsEnabled = true,
                Name = nameof(TestRailsXmlCase.Type),
                IsRequired = false,
                Type = CustomAttributeTypesEnum.Options,
                Options = typeElements.Select(xElement => new CustomAttributeOptionModel
                {
                    Value = xElement.Value
                }).DistinctBy(x => x.Value).ToList()
            });
        }

        return attributesScheme;
    }

    private static CustomAttributeTypesEnum GetAttributeType(IGrouping<string, XElement> attributeGroup)
    {
        var isCheck = attributeGroup
            .All(xElement => xElement.Elements().Count() == 2
                && xElement.Elements().All(xxElement => xxElement.Name == "id" || xxElement.Name == "value"));

        if (isCheck)
        {
            return CustomAttributeTypesEnum.Options;
        }

        isCheck = attributeGroup
            .All(xElement => xElement.HasElements && xElement.Elements().All(xxElement => xxElement.Name == "item"
                                                             && xxElement.Elements().Count() == 2
                                                             && xxElement.Elements().All(xxxElement =>
                                                                 xxxElement.Name == "id" || xxxElement.Name == "value")));

        if (isCheck)
        {
            return CustomAttributeTypesEnum.MultipleOptions;
        }

        isCheck = attributeGroup.All(xElement => xElement.IsEmpty == false && bool.TryParse(xElement.Value, out _));

        return isCheck ? CustomAttributeTypesEnum.CheckBox : CustomAttributeTypesEnum.String;
    }
}
