using System.Management;
using System.Text.RegularExpressions;
using ZephyrScaleServerExporter.Models.Attachment;

namespace ZephyrScaleServerExporter.Services.Helpers;

public static partial class Utils
{
    private const string ImgPattern = "<img[^>]*>";
    private const string UrlPattern = @"src=""\.\.([^""]+)""";
    private const string HyperlinkPattern = @"<a [^>]*>";
    private const string HyperlinkUrlPattern = @"href=""([^\s]+)""";
    private const string HtmlPattern = @"<.*?>";
    private const string FormatTabCharacter = "\t";
    private const string FormatNewLineCharacter = "\n";
    private static int _logicalCoreCount = 0;

    public static string ReplaceInvalidChars(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));    
    }

    private static int CalcLogicalProcessors()
    {
        if (!OperatingSystem.IsWindows())
        {
            return 2;
        }
        try
        {
            const string wmiQuery = "Select * from Win32_ComputerSystem";
            var obj = new ManagementObjectSearcher(wmiQuery).Get().Cast<ManagementBaseObject>().First();
            return int.Parse(obj["NumberOfLogicalProcessors"].ToString() ?? string.Empty);
        }
        catch (Exception)
        {
            // ignored
        }
        return 2;
    }
    
    public static int GetLogicalProcessors()
    {
        if (_logicalCoreCount != 0) return _logicalCoreCount;
        _logicalCoreCount = CalcLogicalProcessors();
        return _logicalCoreCount;
    }
    
    public static string SpacesToUnderscores(string input)
    {
        if (input.Contains(' '))
        {
            return input.Replace(" ", "_");
        }
        return input;
    }

    public static void AddIfUnique<T>(List<T> list, T item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);    
        }
    }

    public static void AddIfUnique<T>(List<T> list, List<T> items)
    {
        foreach (var item in items)
        {
            AddIfUnique<T>(list, item);
        }
    }
    
    public static ZephyrDescriptionData ExtractAttachments(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return new ZephyrDescriptionData
            {
                Description = string.Empty,
                Attachments = new List<ZephyrAttachment>()
            };
        }

        var data = new ZephyrDescriptionData
        {
            Description = description,
            Attachments = new List<ZephyrAttachment>()
        };

        var imgRegex = ImgPatternRegex();

        var matches = imgRegex.Matches(description);

        if (matches.Count == 0)
        {
            return data;
        }

        foreach (Match match in matches)
        {
            var urlRegex = UrlPatternRegex();
            var urlMatch = urlRegex.Match(match.Value);

            if (!urlMatch.Success) continue;

            var url = urlMatch.Groups[1].Value;
            var urlWords = url.Split('/');
            var fileName = urlWords[^1];
            var transformedName = SpacesToUnderscores(fileName);
            data.Description = data.Description.Replace(match.Value, $"<<<{transformedName}>>>");
            data.Attachments.Add(new ZephyrAttachment
            {
                FileName = transformedName,
                Url = url
            });
        }

        return data;
    }

    public static string ExtractHyperlinks(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        var hyperlinkRegex = HyperlinkPatternRegex();

        var matches = hyperlinkRegex.Matches(description);

        if (matches.Count == 0)
        {
            return description;
        }

        foreach (Match match in matches)
        {
            var urlRegex = HyperlinkUrlPatternRegex();
            var urlMatch = urlRegex.Match(match.Value);

            if (!urlMatch.Success) continue;

            var url = urlMatch.Groups[1].Value;

            description = description.Replace(match.Value, "[" + url + "]");
        }

        var htmlRegex = HtmlPatternRegex();
        var htmlMatches = htmlRegex.Matches(description);

        foreach (Match htmlMatch in htmlMatches)
        {
            description = description.Replace(htmlMatch.Value, "");
        }

        return description;
    }

    public static string ConvertingFormatCharacters(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        var descriptionLines = description.Split(FormatNewLineCharacter);

        description = string.Join("", descriptionLines.Select(l => $"<p class=\"tiptap-text\">{l}</p>"));
        description = description.Replace(FormatTabCharacter, "    ");

        return description;
    }

    [GeneratedRegex(ImgPattern)]
    private static partial Regex ImgPatternRegex();
    [GeneratedRegex(UrlPattern)]
    private static partial Regex UrlPatternRegex();
    [GeneratedRegex(HyperlinkPattern)]
    private static partial Regex HyperlinkPatternRegex();
    [GeneratedRegex(HyperlinkUrlPattern)]
    private static partial Regex HyperlinkUrlPatternRegex();
    [GeneratedRegex(HtmlPattern)]
    private static partial Regex HtmlPatternRegex();
}
