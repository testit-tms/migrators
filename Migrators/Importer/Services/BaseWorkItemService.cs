using System;
using System.ComponentModel;
using System.Text.Json;
using Importer.Models;
using Models;

namespace Importer.Services;

public abstract class BaseWorkItemService
{
    private const string OptionsType = "options";
    private const string MultipleOptionsType = "multipleOptions";
    private const string Checkbox = "checkbox";

    protected static List<CaseAttribute> ConvertAttributes(IEnumerable<CaseAttribute> attributes,
        Dictionary<Guid, TmsAttribute> tmsAttributes)
    {
        var list = new List<CaseAttribute>();

        foreach (var attribute in attributes)
        {
            var tmsAttribute = tmsAttributes[attribute.Id];
            var value = ConvertAttributeValue(tmsAttribute, attribute);

            list.Add(new CaseAttribute { Id = tmsAttribute.Id, Value = value });
        }

        return list;
    }

    private static object ConvertAttributeValue(TmsAttribute tmsAttribute, CaseAttribute caseAttribute)
    {
        if (string.Equals(tmsAttribute.Type, OptionsType, StringComparison.InvariantCultureIgnoreCase))
        {
            return Enumerable.FirstOrDefault(tmsAttribute.Options, o => o.Value == caseAttribute.Value.ToString())?.Id.ToString();
        }
        else if (string.Equals(tmsAttribute.Type, MultipleOptionsType, StringComparison.InvariantCultureIgnoreCase))
        {
            var ids = new List<string>();
            var options = JsonSerializer.Deserialize<List<string>>(caseAttribute.Value.ToString());

            foreach (var option in options)
            {
                ids.Add(Enumerable.FirstOrDefault(tmsAttribute.Options, o => o.Value == option)?.Id.ToString());
            }

            return ids;
        }
        else if (string.Equals(tmsAttribute.Type, Checkbox, StringComparison.InvariantCultureIgnoreCase))
        {
            return bool.Parse(caseAttribute.Value.ToString());
        }

        if (Guid.TryParse(caseAttribute.Value.ToString(), out _))
        {
            return "uuid " + caseAttribute.Value.ToString();
        }

        return caseAttribute.Value.ToString();
    }

    /// <summary>
    /// Searches for a specific value within an HTML string (input).
    /// If the value is found inside an HTML tag, the function moves this value to a position
    /// after the closing tag of the current HTML element.
    /// </summary>
    private static string UpImageLinkIfNeeded(string source, string matchValue)
    {
        try
        {
            // Find the position of the value in the string
            int valueIndex = source.IndexOf(matchValue, StringComparison.Ordinal);
            if (valueIndex == -1)
            {
                return source; // If value is not found, return input unchanged
            }

            // Find the last opening tag before the value
            int openingTagStart = source.LastIndexOf('<', valueIndex);
            int openingTagEnd = source.IndexOf('>', openingTagStart);
            if (openingTagStart == -1 || openingTagEnd == -1)
            {
                return source; // If the structure is invalid, return input unchanged
            }

            // Extract the tag name
            string tagName = source.Substring(openingTagStart + 1,
                openingTagEnd - openingTagStart - 1).Split(' ')[0];

            // Find the corresponding closing tag
            string closingTag = $"</{tagName}>";
            int closingTagIndex = source.IndexOf(closingTag, valueIndex, StringComparison.Ordinal);
            if (closingTagIndex == -1)
            {
                return source; // If no closing tag found, return input unchanged
            }

            int closingTagLength = closingTag.Length;

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

    private static string HandleStepImageLink(string source, string a, Dictionary<string, Guid> attachments)
    {
        if (source.Contains($"<<<{a}>>>"))
        {
            source = source.Replace($"<<<{a}>>>", $"%%%{a}%%%");
            source = UpImageLinkIfNeeded(source, $"%%%{a}%%%");
            source = source.Replace($"%%%{a}%%%", $"<p> <img src=\"/api/Attachments/{attachments[a]}\"> </p>");
        }
        else
        {
            if (IsImage(a))
            {
                source += $" <p> <img src=\"/api/Attachments/{attachments[a]}\"> </p>";
            }
            else
            {
                source += $" <p> File attached to test case: {a} </p>";
            }
        }
        return source;
    }

    protected static List<Step> AddAttachmentsToSteps(List<Step> steps, Dictionary<string, Guid> attachments)
    {
        steps.ToList().ForEach(
            s =>
            {
                s.ActionAttachments?.ForEach(a =>
                {
                    s.Action = HandleStepImageLink(s.Action, a, attachments);
                });

                s.ExpectedAttachments?.ForEach(a =>
                {
                    s.Expected = HandleStepImageLink(s.Expected, a, attachments);
                });

                s.TestDataAttachments?.ForEach(a =>
                {
                    s.TestData = HandleStepImageLink(s.TestData, a, attachments);
                });
            });

        return steps;
    }

    protected static bool IsImage(string name)
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
