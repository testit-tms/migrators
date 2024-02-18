using System.Xml.Linq;
using System.Xml.Serialization;
using TestRailImporter.Enums;
using TestRailImporter.Models;

namespace TestRailImporter.Services;

public class TestRailReader
{
    private readonly XmlSerializer _xmlSerializer;

    public TestRailReader(XmlSerializer xmlSerializer)
    {
        _xmlSerializer = xmlSerializer;
    }

    public TestRailsXmlSuite Read(Stream fileStream)
    {
        return (TestRailsXmlSuite)_xmlSerializer.Deserialize(fileStream)!;
    }

    public static async Task<List<CustomAttributeModel>> GetCustomAttributesAsync(Stream fileStream)
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

        var xml = await XDocument.LoadAsync(fileStream, LoadOptions.None, default).ConfigureAwait(false);

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
                Options = new List<CustomAttributeOptionModel>()
            };

            switch (attributeType)
            {
                case CustomAttributeTypesEnum.MultipleOptions:
                    {
                        attributeModel.IsGlobal = true;

                        var values = attributeGroup
                            .SelectMany(xElement => xElement.Elements("item"))
                            .OrderBy(xElement => long.Parse(xElement.Element("id")?.Value ?? string.Empty))
                            .Select(xElement => xElement.Element("value")?.Value ?? string.Empty)
                            .Distinct()
                            .ToList();

                        attributeModel.Options.AddRange(values.Select(value => new CustomAttributeOptionModel
                            { Value = value }));

                        break;
                    }

                case CustomAttributeTypesEnum.Options:
                    {
                        var values = attributeGroup
                            .OrderBy(xElement => long.Parse(xElement.Element("id")?.Value ?? string.Empty))
                            .Select(xElement => xElement.Element("value")?.Value ?? string.Empty)
                            .Distinct()
                            .ToList();

                        attributeModel.Options.AddRange(values.Select(value => new CustomAttributeOptionModel
                            { Value = value }));

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
            attributesScheme.Add(new CustomAttributeModel
            {
                IsEnabled = true,
                Name = nameof(TestRailsXmlCase.References),
                Options = new List<CustomAttributeOptionModel>(),
                IsRequired = false,
                Type = CustomAttributeTypesEnum.String
            });

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
