using System.Text.RegularExpressions;
using QaseExporter.Models;

namespace QaseExporter.Services;

public static class Utils
{
    private const string ImgPattern = @"!\[[^\[\]]*\]\([^()\s]*\)";
    private const string UrlPattern = @"\(([^()\s]+)\)";
    private const string HyperlinkPattern = @"\[[^\[\]]*\]\([^()\s]*\)";
    private const string titlePattern = @"\[([^\[\]]+)\]";
    private const string ToggleStrongStrPattern = @"\*\*(.*?)\*\*";  // Match: "**{anything}**"
    private const string ToggleStrikethroughStrPattern = @"\~\~(.*?)\~\~";  // Match: "~~{anything}~~"
    private const string BlockCodeStrPattern = @"\`{3}([\s\S]*?)\`{3}"; // Match: "```{anything}```"
    private const string CodeStrPattern = @"(?<!`)\`(.*?)\`(?!`)"; // Match: "`{anything}`", No match: "```{anything}```"
    private const string BackslashCharacterPattern = @"(?<!\\)\\(?!\\)"; // Match: "\\", No match: "\\\\"
    private const string ToggleStrongCharacterPattern = @"\*\*"; // Match: "**"
    private const string ToggleStrikethroughCharacterPattern = @"\~\~"; // Match: "~~"
    private const string CodeCharacterPattern = @"(?<!\\)\`"; // Match: "`", No match: "\\`"
    private const string AngleBracketCharacterPattern = @"\\<"; // Match: "\<", No match: "<"
    private const string FormatTabCharacter = "\t";
    private const string FormatNewLineCharacter = "\n";

    public static QaseDescriptionData ExtractAttachments(string? description)
    {
        var data = new QaseDescriptionData
        {
            Description = description,
            Attachments = new List<QaseAttachment>()
        };

        var matches = GetAllMatchesByPattern(description, ImgPattern);

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
        var matches = GetAllMatchesByPattern(description, HyperlinkPattern);

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

    public static string ConvertingToggleStrongStr(string? description)
    {
        var matches = GetAllMatchesByPattern(description, ToggleStrongStrPattern);

        if (matches.Count == 0)
        {
            return description;
        }

        foreach (Match match in matches)
        {
            var matchWithoutToggleStrongFormat = RemoveToggleStrongCharacters(match.Value);
            description = description.Replace(match.Value, $"<strong>{matchWithoutToggleStrongFormat}</strong>");
        }

        return description;
    }

    public static string ConvertingToggleStrikethroughStr(string? description)
    {
        var matches = GetAllMatchesByPattern(description, ToggleStrikethroughStrPattern);

        if (matches.Count == 0)
        {
            return description;
        }

        foreach (Match match in matches)
        {
            var matchWithoutToggleStrikethroughFormat = RemoveToggleStrikethroughCharacters(match.Value);
            description = description.Replace(match.Value, $"<s>{matchWithoutToggleStrikethroughFormat}</s>");
        }

        return description;
    }

    public static string ConvertingBlockCodeStr(string? description)
    {
        var matches = GetAllMatchesByPattern(description, BlockCodeStrPattern);

        if (matches.Count == 0)
        {
            return description;
        }

        foreach (Match match in matches)
        {
            var matchWithoutBlockCodeFormat = RemoveCodeCharacters(match.Value);
            description = description.Replace(match.Value, $"<pre class=\"tiptap-code-block\"><code>{matchWithoutBlockCodeFormat}</code></pre>");
        }

        return description;
    }

    public static string ConvertingCodeStr(string? description)
    {
        var matches = GetAllMatchesByPattern(description, CodeStrPattern);

        if (matches.Count == 0)
        {
            return description;
        }

        foreach (Match match in matches)
        {
            var matchWithoutCodeFormat = RemoveCodeCharacters(match.Value);
            description = description.Replace(match.Value, $"<code class=\"tiptap-inline-code\">{matchWithoutCodeFormat}</code>");
        }

        return description;
    }

    public static string ConvertingFormatCharactersWithBlockCodeStr(string? description)
    {
        var matches = GetAllMatchesByPattern(description, BlockCodeStrPattern);

        if (matches.Count == 0)
        {
            return description;
        }

        var remainingDescription = description;
        var convertedDescription = string.Empty;

        foreach (Match match in matches)
        {
            var descriptionsBetweenBlockCode = remainingDescription.Split(match.Value);

            convertedDescription += ConvertingFormatCharacters(descriptionsBetweenBlockCode[0]) + match.Value;

            remainingDescription = descriptionsBetweenBlockCode[1];
        }

        convertedDescription += ConvertingFormatCharacters(remainingDescription);

        return convertedDescription;
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

    public static string ConvertingAngleBracketsStr(string? description)
    {
        var matches = GetAllMatchesByPattern(description, AngleBracketCharacterPattern);

        if (matches.Count == 0)
        {
            return description;
        }

        foreach (Match match in matches)
        {
            description = description.Replace(match.Value, "&lt;");
        }

        return description;
    }

    private static List<Match> GetAllMatchesByPattern(string? description, string pattern)
    {
        if (string.IsNullOrEmpty(description))
        {
            return new List<Match>();
        }

        var regex = new Regex(pattern);

        return regex.Matches(description).ToList();
    }

    public static string RemoveBackslashCharacters(string? description)
    {
        return RemoveCharactersFromDescriptionByPattern(description, BackslashCharacterPattern);
    }

    public static string RemoveToggleStrongCharacters(string? description)
    {
        return RemoveCharactersFromDescriptionByPattern(description, ToggleStrongCharacterPattern);
    }

    public static string RemoveToggleStrikethroughCharacters(string? description)
    {
        return RemoveCharactersFromDescriptionByPattern(description, ToggleStrikethroughCharacterPattern);
    }

    public static string RemoveCodeCharacters(string? description)
    {
        return RemoveCharactersFromDescriptionByPattern(description, CodeCharacterPattern);
    }

    private static string RemoveCharactersFromDescriptionByPattern(string? description, string pattern)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        var regex = new Regex(pattern);

        return regex.Replace(description, "");
    }
}
