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

        if (Guid.TryParse(caseAttribute.Value.ToString(), out _))
        {
            return "uuid " + caseAttribute.Value.ToString();
        }

        return caseAttribute.Value.ToString();
    }

    protected static List<Step> AddAttachmentsToSteps(List<Step> steps, Dictionary<string, Guid> attachments)
    {
        steps.ToList().ForEach(
            s =>
            {
                s.ActionAttachments?.ForEach(a =>
                {
                    if (s.Action.Contains($"<<<{a}>>>"))
                    {
                        s.Action = s.Action.Replace($"<<<{a}>>>", $"<p> <img src=\"/api/Attachments/{attachments[a]}\"> </p>");
                    }
                    else
                    {
                        if (IsImage(a))
                        {
                            s.Action += $" <p> <img src=\"/api/Attachments/{attachments[a]}\"> </p>";
                        }
                        else
                        {
                            s.Action += $" <p> File attached to test case: {a} </p>";
                        }
                    }
                });

                s.ExpectedAttachments?.ForEach(a =>
                {
                    if (s.Expected.Contains($"<<<{a}>>>"))
                    {
                        s.Expected = s.Expected.Replace($"<<<{a}>>>", $"<p> <img src=\"/api/Attachments/{attachments[a]}\"> </p>");
                    }
                    else
                    {
                        if (IsImage(a))
                        {
                            s.Expected += $" <p> <img src=\"/api/Attachments/{attachments[a]}\"> </p>";
                        }
                        else
                        {
                            s.Expected += $" <p> File attached to test case: {a} </p>";
                        }
                    }
                });

                s.TestDataAttachments?.ForEach(a =>
                {
                    if (s.TestData.Contains($"<<<{a}>>>"))
                    {
                        s.TestData = s.TestData.Replace($"<<<{a}>>>", $"<p> <img src=\"/api/Attachments/{attachments[a]}\"> </p>");
                    }
                    else
                    {
                        if (IsImage(a))
                        {
                            s.TestData += $" <p> <img src=\"/api/Attachments/{attachments[a]}\"> </p>";
                        }
                        else
                        {
                            s.TestData += $" <p> File attached to test case: {a} </p>";
                        }
                    }
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
