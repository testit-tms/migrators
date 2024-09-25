using System.Text.RegularExpressions;
using QaseExporter.Models;

namespace QaseExporter.Services;

public static class Utils
{
    private const string ImgPattern = @"!\[[^\[\]]*\]\([^()\s]*\)";
    private const string UrlPattern = @"\(([^()\s]+)\)";
    private const string HyperlinkPattern = @"\[[^\[\]]*\]\([^()\s]*\)";
    private const string titlePattern = @"\[([^\[\]]+)\]";
    private const string BackslashCharacterPattern = @"(?<!\\)\\(?!\\)";
    private const string FormatTabCharacter = "\t";
    private const string FormatNewLineCharacter = "\n";

    public static QaseDescriptionData ExtractAttachments(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return new QaseDescriptionData
            {
                Description = string.Empty,
                Attachments = new List<QaseAttachment>()
            };
        }

        var data = new QaseDescriptionData
        {
            Description = description,
            Attachments = new List<QaseAttachment>()
        };

        var imgRegex = new Regex(ImgPattern);

        var matches = imgRegex.Matches(description);

        if (matches.Count == 0)
        {
            return data;
        }

        foreach (Match match in matches)
        {
            var urlRegex = new Regex(UrlPattern);
            var urlMatch = urlRegex.Match(match.Value);

            if (!urlMatch.Success) continue;

            var url = urlMatch.Groups[1].Value;
            var fileName = url.Split('/').Last();

            data.Description = data.Description.Replace(match.Value, $"<<<{fileName}>>>");
            data.Attachments.Add(new QaseAttachment
            {
                Name = fileName,
                Url = url
            });
        }

        return data;
    }

    public static string ConvertingHyperlinks(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        var hyperlinkRegex = new Regex(HyperlinkPattern);

        var matches = hyperlinkRegex.Matches(description);

        if (matches.Count == 0)
        {
            return description;
        }

        foreach (Match match in matches)
        {
            var urlRegex = new Regex(UrlPattern);
            var urlMatch = urlRegex.Match(match.Value);

            if (!urlMatch.Success) continue;

            var url = urlMatch.Groups[1].Value;

            var titleRegex = new Regex(titlePattern);
            var titleMatch = titleRegex.Match(match.Value);

            var title = titleMatch.Success ? titleMatch.Groups[1].Value : url;

            description = description.Replace(match.Value, $"<a target=\"_blank\" rel=\"noopener noreferrer\" href=\"{url}\">{title}</a>");
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

    public static string RemoveBackslashCharacters(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        var backslashCharacterRegex = new Regex(BackslashCharacterPattern);

        return backslashCharacterRegex.Replace(description, "");
    }
}
