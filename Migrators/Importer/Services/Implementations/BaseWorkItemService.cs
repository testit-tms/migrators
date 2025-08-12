using System.Text.Json;
using Importer.Client;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services;

internal class BaseWorkItemService(
    ILogger<BaseWorkItemService> logger, 
    IClientAdapter clientAdapter)
    : IBaseWorkItemService
{
    private const string OptionsType = "options"; // one value
    private const string MultipleOptionsType = "multipleOptions"; // array of values
    private const string Checkbox = "checkbox"; // true / false

    public async Task<List<CaseAttribute>> ConvertAttributes(IEnumerable<CaseAttribute> attributes,
        Dictionary<Guid, TmsAttribute> tmsAttributes)
    {
        var list = new List<CaseAttribute>();

        foreach (var attribute in attributes)
        {
            var tmsAttribute = tmsAttributes[attribute.Id];
            var (value, newAttribute) = await ConvertAttributeValue(tmsAttribute, attribute);
            tmsAttributes[attribute.Id] = newAttribute;
            list.Add(new CaseAttribute { Id = newAttribute.Id, Value = value });
        }

        return list;
    }

    private async Task<(object, TmsAttribute)> ConvertAttributeValue(TmsAttribute tmsAttribute, CaseAttribute caseAttribute)
    {
        if (string.Equals(tmsAttribute.Type, OptionsType, StringComparison.InvariantCultureIgnoreCase))
        {
            var result = tmsAttribute.Options.FirstOrDefault(o => o.Value == caseAttribute.Value.ToString())?.Id.ToString()!;
            return (result, tmsAttribute);
        }
            
        if (string.Equals(tmsAttribute.Type, MultipleOptionsType, StringComparison.InvariantCultureIgnoreCase))
        {
            var ids = new List<string>();
            var options = JsonSerializer.Deserialize<List<string>>(caseAttribute.Value.ToString()!)!;

            foreach (var option in options)
            {
                var foundValue = tmsAttribute.Options.FirstOrDefault(o => o.Value == option);
                if (foundValue != null)
                {
                    ids.Add(foundValue.Id.ToString());    
                }
                else if (option != "")
                {
                    var logMessage = $"Option ${option} not found in ${tmsAttribute.Id} ${tmsAttribute.Name} " +
                                     $"- add it dynamically";
                    logger.LogWarning(logMessage);
                    tmsAttribute.Options.Add(new TmsAttributeOptions
                    {
                        Value = option,
                        IsDefault = false
                    });
                    await clientAdapter.UpdateAttribute(tmsAttribute);
                    tmsAttribute = await clientAdapter.GetProjectAttributeById(tmsAttribute.Id);
                    ids.Add(option);    
                }
            }
                
            return (ids, tmsAttribute);
        }

        if (string.Equals(tmsAttribute.Type, Checkbox, StringComparison.InvariantCultureIgnoreCase))
        {
            var result = bool.Parse(caseAttribute.Value.ToString()!);
            return (result, tmsAttribute);
        }


        if (Guid.TryParse(caseAttribute.Value.ToString(), out _))
        {
            var result = "uuid " + caseAttribute.Value;
            return (result, tmsAttribute);
        }

        return (caseAttribute.Value.ToString()!, tmsAttribute);
    }

    /// <summary>
    ///     Searches for a specific value within an HTML string (input).
    ///     If the value is found inside an HTML tag, the function moves this value to a position
    ///     after the closing tag of the current HTML element.
    /// </summary>
    private static string UpImageLinkIfNeeded(string source, string matchValue)
    {
        try
        {
            // Find the position of the value in the string
            var valueIndex = source.IndexOf(matchValue, StringComparison.Ordinal);
            if (valueIndex == -1) return source; // If value is not found, return input unchanged

            // Find the last opening tag before the value
            var openingTagStart = source.LastIndexOf('<', valueIndex);
            var openingTagEnd = source.IndexOf('>', openingTagStart);
            if (openingTagStart == -1 ||
                openingTagEnd == -1) return source; // If the structure is invalid, return input unchanged

            // Extract the tag name
            var tagName = source.Substring(openingTagStart + 1,
                openingTagEnd - openingTagStart - 1).Split(' ')[0];

            // Find the corresponding closing tag
            var closingTag = $"</{tagName}>";
            var closingTagIndex = source.IndexOf(closingTag, valueIndex, StringComparison.Ordinal);
            if (closingTagIndex == -1) return source; // If no closing tag found, return input unchanged

            var closingTagLength = closingTag.Length;

            // Remove the value from its current position
            source = source.Remove(valueIndex, matchValue.Length);

            // Recalculate the position of the closing tag after the removal
            closingTagIndex = source.IndexOf(closingTag, openingTagStart,
                StringComparison.Ordinal) + closingTagLength;

            // Insert the value after the identified closing tag
            source = source.Insert(closingTagIndex, matchValue);

            return source;
        }
        catch (Exception)
        {
            return source;
        }
    }

    private static string HandleStepImageLink(string source, string attachName, Dictionary<string, Guid> attachments)
    {
        var isOk = attachments.TryGetValue(attachName, out var attachGuid);
        // fail fast: if there is a broken link -> delete this broken link
        if (!isOk)
        {
            if (source.Contains($"<<<{attachName}>>>"))
            {
                source = source.Replace($"<<<{attachName}>>>", $"");
            }

            return source;
        }
        
        if (source.Contains($"<<<{attachName}>>>"))
        {
            source = source.Replace($"<<<{attachName}>>>", $"%%%{attachName}%%%");
            source = UpImageLinkIfNeeded(source, $"%%%{attachName}%%%");
            source = source.Replace($"%%%{attachName}%%%", $"<p> <img src=\"/api/Attachments/{attachGuid}\"> </p>");
        }
        else
        {
            if (IsImage(attachName))
                source += $" <p> <img src=\"/api/Attachments/{attachGuid}\"> </p>";
            else
                source += $" <p> File attached to test case: {attachName} </p>";
        }

        return source;
    }

    public List<Step> AddAttachmentsToSteps(List<Step> steps, Dictionary<string, Guid> attachments)
    {
        steps.ToList().ForEach(
            s =>
            {
                s.ActionAttachments?.ForEach(a => { s.Action = HandleStepImageLink(s.Action, a, attachments); });

                s.ExpectedAttachments?.ForEach(a => { s.Expected = HandleStepImageLink(s.Expected, a, attachments); });

                s.TestDataAttachments?.ForEach(a => { s.TestData = HandleStepImageLink(s.TestData, a, attachments); });
            });

        return steps;
    }

    private static bool IsImage(string name)
    {
        return Path.GetExtension(name) switch
        {
            ".jpg" => true,
            ".jpeg" => true,
            ".png" => true,
            _ => false
        };
    }
}