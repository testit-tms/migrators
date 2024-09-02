using System.Text.RegularExpressions;
using QaseExporter.Models;

namespace QaseExporter.Services;

public static class Utils
{
    private const string ImgPattern = @"!\[[\S]*\]\([\S]*\)";
    private const string UrlPattern = @"\(([\S]+)\)";

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
}
