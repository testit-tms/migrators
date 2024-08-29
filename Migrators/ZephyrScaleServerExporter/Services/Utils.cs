using System.Text.RegularExpressions;
using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services;

public static class Utils
{
    private const string ImgPattern = "<img[^>]*>";
    private const string UrlPattern = @"src=""\.\.([^""]+)""";

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
            data.Attachments.Add(new ZephyrAttachment
            {
                FileName = fileName,
                Url = url
            }); ;
        }

        return data;
    }
}
