using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using ZephyrScaleServerExporter.AttrubuteMapping;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.TestCases;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;


namespace ZephyrScaleServerExporter.Services.TestCase.Implementations;

internal class TestCaseAttributesService(
    IDetailedLogService detailedLogService,
    ILogger<TestCaseConvertService> logger,
    IMappingConfigReader mappingConfigReader)
    : ITestCaseAttributesService
{
    public List<CaseAttribute> CalculateAttributes(ZephyrTestCase zephyrTestCase,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames)
    {
        var attributes = new List<CaseAttribute>();
        if (!string.IsNullOrEmpty(zephyrTestCase.Component))
        {
            attributes.Add(
                new CaseAttribute
                {
                    Id = attributeMap[Constants.ComponentAttribute].Id,
                    Value = zephyrTestCase.Component
                }
            );
        }
        RemapStatusAttribute(zephyrTestCase, "Состояние");
        attributes.AddRange(
            [
                new CaseAttribute
                {
                    Id = attributeMap[Constants.IdZephyrAttribute].Id,
                    Value = zephyrTestCase.Key
                },
                new CaseAttribute
                {
                    Id = attributeMap[Constants.ZephyrStatusAttribute].Id,
                    Value = zephyrTestCase.Status
                }
            ]
        );
        var (_, updatedRequiredNames) = MakeAttributesNotRequiredFromTestCase(
            zephyrTestCase, attributes, attributeMap, requiredAttributeNames.ToImmutableList());
        requiredAttributeNames.Clear();
        requiredAttributeNames.AddRange(updatedRequiredNames);

        PopulateAttributesWithEmptyCheckboxes(attributes, attributeMap);

        return attributes;
    }

    // all clients fix: https://work.teamstorm.io/tasks/item/TMS-31715
    // Get all non required checkbox values from project's attributeMap
    // check current 'attributes' list for presenting there values from previous list
    // if no - add them.
    private static void PopulateAttributesWithEmptyCheckboxes(
        List<CaseAttribute> attributes,
        Dictionary<string, Attribute> attributeMap)
    {
        var allNotUsedAttributes = attributeMap
            .Where(x =>
                x.Value is { Type: AttributeType.Checkbox, IsRequired: false })
            .Where(x =>
                attributes.Find(a => a.Id == x.Value.Id) == null);

        attributes.AddRange(
            allNotUsedAttributes.Select(x => new CaseAttribute
            {
                Id = x.Value.Id,
                Value = "False"
            })
        );
    }

    /// <summary>
    /// Make attributeMap filled with isRequired = false attributes if needed
    /// (some attributes are not present in current testCase)
    /// </summary>
    /// <param name="zephyrTestCase"></param>
    /// <param name="attributes">attributes to be updated from <see cref="ZephyrTestCase.CustomFields"/> </param>
    /// <param name="attributeMap">map of attributes to be updated with isRequired = false</param>
    /// <param name="newRequiredAttributeNames"></param>
    /// <returns>(attributeMap, usedRequiredAttributeNames)</returns>
    private (Dictionary<string,Attribute>, ImmutableList<string>) MakeAttributesNotRequiredFromTestCase(
        ZephyrTestCase zephyrTestCase, List<CaseAttribute> attributes,
        Dictionary<string,Attribute> attributeMap,  ImmutableList<string> newRequiredAttributeNames)
    {
        if (zephyrTestCase.CustomFields == null || zephyrTestCase.CustomFields.Count == 0)
        {
            detailedLogService.LogDebug("no custom fields - no any new required attributes");
            SetAttributesNotRequiredFromList(attributeMap, newRequiredAttributeNames);
            return (attributeMap, []);
        }

        // add custom attributes
        attributes.AddRange(ConvertAttributes(zephyrTestCase.CustomFields!, attributeMap));

        var (unusedRequiredAttributeNames, usedRequiredAttributeNames)
            = GetUnusedRequiredAttributes(zephyrTestCase, newRequiredAttributeNames);

        detailedLogService.LogDebug("UnusedRequiredAttributeNames: {List}", unusedRequiredAttributeNames);
        SetAttributesNotRequiredFromList(attributeMap, unusedRequiredAttributeNames);
        detailedLogService.LogDebug("AttributeMap: {List}", attributeMap);

        return (attributeMap, usedRequiredAttributeNames);
    }


    /// <summary>
    /// change status attribute in place to it's remapping from mapping.json file
    /// </summary>
    private void RemapStatusAttribute(ZephyrTestCase zephyrTestCase, string attributeName)
    {
        try
        {
            mappingConfigReader.InitOnce("mapping.json", "");
            var mappingValue = mappingConfigReader
                .GetMappingForValue(zephyrTestCase.Status ?? "", attributeName);
            if (mappingValue != null)
            {
                logger.LogInformation($"Map {zephyrTestCase.Status} to {mappingValue} in the attribute {attributeName}");
                zephyrTestCase.Status = mappingValue;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while mapping attribute value or read the file");
        }
    }

    private (ImmutableList<string>, ImmutableList<string>) GetUnusedRequiredAttributes(
        ZephyrTestCase zephyrTestCase, ImmutableList<string> requiredAttributeNames)
    {
        // make a copy for consistency
        var newRequiredAttributeNames = requiredAttributeNames.Select(x => x).ToList();

        // all matched C = (A && B)
        var usedRequiredAttributeNames = zephyrTestCase.CustomFields!.Keys
            .Where(n => newRequiredAttributeNames.Contains(n)).ToList();
        detailedLogService.LogDebug("UsedRequiredAttributeNames: {List}", usedRequiredAttributeNames);
        detailedLogService.LogDebug("NewRequiredAttributeNames: {List}", newRequiredAttributeNames);
        // remove all matched from B, B = (B xor C)
        newRequiredAttributeNames.RemoveAll(n => usedRequiredAttributeNames.Contains(n));
        return (newRequiredAttributeNames.ToImmutableList(), usedRequiredAttributeNames.ToImmutableList());
    }

    private void SetAttributesNotRequiredFromList(
        Dictionary<string, Attribute> attributeMap,
        ImmutableList<string> matchingList)
    {
        logger.LogInformation("Checking required attributes");
        foreach (var unusedRequiredAttributeName in matchingList)
        {
            logger.LogInformation("Required attribute {Name} is not used. Set as optional", unusedRequiredAttributeName);
            var attribute = attributeMap[unusedRequiredAttributeName];
            attribute.IsRequired = false;
            attributeMap[unusedRequiredAttributeName] = attribute;
        }
    }

    private List<CaseAttribute> ConvertAttributes(Dictionary<string, object?> fields,
        Dictionary<string, Attribute> attributeMap)
    {
        detailedLogService.LogDebug("Converting attributes for test case");
        var attributes = new List<CaseAttribute>();

        foreach (var field in fields)
        {
            detailedLogService.LogInformation("Converting attribute {Attribute}:", field);

            if (!attributeMap.TryGetValue(field.Key, out var attribute))
            {
                detailedLogService.LogDebug("The attribute \"{Key}\" cannot be obtained from the attribute map", field.Key);

                continue;
            }

            var zephyrValue = field.Value == null ? string.Empty : field.Value.ToString()!;

            attributes.Add(
                new CaseAttribute
                {
                    Id = attribute.Id,
                    Value = attribute.Type == AttributeType.MultipleOptions ?
                        ConvertMultipleValue(zephyrValue, attribute.Options)
                        : zephyrValue,
                }
            );
        }

        detailedLogService.LogDebug("Converted attributes {@Attributes}", attributes);

        return attributes;
    }



    private List<string> ConvertMultipleValue(string attributeValue, List<string> options)
    {
        logger.LogInformation("Converting multiple value {Value} with options {@Options}", attributeValue, options);

        var testCaseValues = new List<string>();

        var selectedOptions = options
            .Where(option =>
                attributeValue.Contains(option)
                && (attributeValue.Contains(option + ", ")
                    || attributeValue.Contains(", " + option)
                    || attributeValue == option));

        foreach (var option in selectedOptions)
        {
            testCaseValues.Add(option);
            detailedLogService.LogInformation("The option \"{Option}\" add to multiple choice for test case", option);
        }

        detailedLogService.LogInformation("Converted multiple value {Value} to options {@Options}", attributeValue, testCaseValues);

        return testCaseValues;
    }
}
