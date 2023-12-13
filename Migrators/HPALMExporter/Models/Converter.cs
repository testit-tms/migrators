using System.Text.RegularExpressions;
using ImportHPALMToTestIT.Models.HPALM;

namespace HPALMExporter.Models;

public static class Converter
{
    private static readonly Regex RemoveHTMLtagsRegex = new("<(?:\"[^\"]*\"['\"]*|'[^']*'['\"]*|[^'\">])+>");

    public static HPALMFolder ToTestFolder(this Entity folderXml)
    {
        var name = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "name").Value;
        var id = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "id").Value;
        var parentId = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "parent-id").Value;

        return new HPALMFolder
        {
            Name = name,
            Id = int.Parse(id),
            ParentId = int.Parse(parentId)
        };
    }

    public static HPALMTest ToTest(this Entity folderXml, IEnumerable<string> attributes)
    {
        var name = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "name").Value;
        var id = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "id").Value;
        var parentId = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "parent-id").Value;
        var description = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "description").Value;
        var type = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "subtype-id").Value;
        var status = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "status").Value;
        var hasAttachments = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "attachment").Value == "Y";
        var isTemplate = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "template").Value == "Y";
        var author = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "owner").Value;
        var creationTime = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "creation-time").Value;
        var attrs = new Dictionary<string, string>();

        foreach (var attribute in attributes)
        {
            var value = folderXml.Fields.Field.SingleOrDefault(f => f.Name == attribute).Value;
            attrs.Add(attribute, value);
        }

        return new HPALMTest
        {
            Name = name,
            Id = int.Parse(id),
            ParentId = int.Parse(parentId),
            Description = formatDescription(description),
            Type = type,
            Status = status,
            HasAttachments = hasAttachments,
            IsTemplate = isTemplate,
            Attrubites = attrs,
            Author = author,
            CreationTime = creationTime
        };
    }

    public static HPALMStep ToStep(this Entity folderXml)
    {
        var name = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "name").Value;
        var id = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "id").Value;
        var parentId = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "parent-id").Value;
        var description = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "description").Value;
        var expected = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "expected").Value;
        var order = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "step-order").Value;
        var linkId = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "link-test").Value;
        var hasAttachments = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "attachment").Value == "Y";

        return new HPALMStep
        {
            Name = name,
            Id = int.Parse(id),
            ParentId = int.Parse(parentId),
            Description = removeHtmlTags(description),
            HasAttachments = hasAttachments,
            Expected = removeHtmlTags(expected),
            Order = int.Parse(order),
            LinkId = string.IsNullOrEmpty(linkId) ? null : int.Parse(linkId)
        };
    }

    public static HPALMAttachment ToAttachment(this Entity folderXml)
    {
        var name = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "name").Value;
        var id = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "id").Value;
        var parentId = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "parent-id").Value;
        var type = name.Contains(".url") ? HPALMAttachmentType.Url : HPALMAttachmentType.File;
        var description = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "description").Value;

        return new HPALMAttachment
        {
            Name = name,
            Id = uint.Parse(id),
            ParentId = uint.Parse(parentId),
            Type = type,
            Description = type == HPALMAttachmentType.Url
                ? removeHtmlTags(description)
                : description
        };
    }

    public static HPALMParameter ToParameter(this Entity folderXml)
    {
        var name = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "name").Value;
        var id = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "id").Value;
        var parentId = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "parent-id").Value;
        var description = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "description").Value;
        var isAssign = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "ref-count").Value == "1";
        var value = folderXml.Fields.Field.SingleOrDefault(f => f.Name == "default-value").Value;

        var formatValue = RemoveHTMLtagsRegex.Replace(value, "");

        return new HPALMParameter
        {
            Name = name,
            Id = uint.Parse(id),
            ParentId = uint.Parse(parentId),
            Description = formatDescription(description),
            IsAssign = isAssign,
            Value = string.IsNullOrEmpty(formatValue) ? "None" : formatValue
        };
    }

    private static string removeHtmlTags(string value)
    {
        return value.Replace("<html><body>", "")
            .Replace("</body></html>", "")
            .TrimStart()
            .TrimEnd();;
    }

    private static string formatDescription(string value)
    {
        return RemoveHTMLtagsRegex.Replace(value, "");
    }

    // static string extractHtmlTable(string value)
    // {
    //     var newStr = value.Replace("&lt;", "<")
    //         .Replace("&gt;", ">")
    //         .Replace("&nbsp;", " ")
    //         .Replace("&quot;", "\"")
    //         .Replace("\n", "")
    //         .Replace("\r", "");
    //
    //     var regex = new Regex(@"(<table.*?</table>\s*)");
    //     var result = regex.Matches(newStr);
    //
    //     if (result.Count == 0)
    //     {
    //         return newStr;
    //     }
    //
    //     var doc = new HtmlDocument();
    //     doc.LoadHtml(newStr);
    //
    //     var nodes = doc.DocumentNode.SelectNodes("/html/body//table//tbody");
    //
    //     if (nodes == null)
    //     {
    //         return newStr;
    //     }
    //
    //     var j = 0;
    //
    //     foreach (var table in nodes)
    //     {
    //         var i = 0;
    //         var tds = new List<string>();
    //         var trs = new Dictionary<int, List<string>>();
    //
    //         foreach (var row in table.SelectNodes("tr"))
    //         {
    //             if (i == 0)
    //             {
    //                 row.SelectNodes("th|td").ToList().ForEach(cell => tds.Add(cell.InnerText));
    //             }
    //             else
    //             {
    //                 var tempList = new List<string>();
    //                 row.SelectNodes("th|td").ToList().ForEach(cell => tempList.Add(cell.InnerText));
    //                 trs.Add(i, tempList);
    //             }
    //
    //             i++;
    //         }
    //
    //         var conTable = new ConsoleTable(tds.ToArray());
    //
    //         foreach (var cell in trs.Values)
    //         {
    //             if (cell.Count < tds.Count)
    //             {
    //                 var count = tds.Count - cell.Count;
    //
    //                 for (var k = 0; k < count; k++)
    //                 {
    //                     cell.Add("");
    //                 }
    //             }
    //
    //             conTable.AddRow(cell.ToArray());
    //         }
    //
    //         var rows = conTable.ToStringAlternative();
    //         var newTable = rows.Split("\n")
    //             .Aggregate("", (current, s) => current + $"<p>{s.Replace(" ", "&nbsp;&nbsp;&nbsp;")}</p>\n");
    //
    //         newStr = newStr.Replace(result[j].Value, $"\n{newTable}\n");
    //         // newStr = newStr.Replace(result[j].Value, $"\n{conTable.ToMinimalString()}\n");
    //
    //         j++;
    //     }
    //
    //     return newStr;
    // }
}
