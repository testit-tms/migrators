using Importer.Models;
using Models;

namespace Importer.Services;

public abstract class BaseWorkItemService
{
    private const string OptionsType = "options";

    protected static List<CaseAttribute> ConvertAttributes(IEnumerable<CaseAttribute> attributes,
        Dictionary<Guid, TmsAttribute> tmsAttributes)
    {
        var list = new List<CaseAttribute>();

        foreach (var attribute in attributes)
        {
            var atr = tmsAttributes[attribute.Id];
            var value = string.Equals(atr.Type, OptionsType, StringComparison.InvariantCultureIgnoreCase)
                ? Enumerable.FirstOrDefault(atr.Options, o => o.Value == attribute.Value)?.Id
                    .ToString()
                : attribute.Value;

            list.Add(new CaseAttribute { Id = atr.Id, Value = value });
        }

        return list;
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
