using System.Text.RegularExpressions;

namespace DoqaExporter.Services;

/// <summary>
/// Обработка HTML-контента из DOQA:
/// - Извлечение base64 картинок в бинарные данные
/// - Конвертация ссылок
/// - Очистка HTML-тегов
/// </summary>
public static class HtmlProcessor
{
    private static readonly Regex Base64ImageRegex = new(
        @"<img\s[^>]*src=""data:image/(\w+);base64,([A-Za-z0-9+/=\s]+)""[^>]*>",
        RegexOptions.Compiled);

    private static readonly Regex LinkRegex = new(
        @"<a\s[^>]*href=""([^""]*)""[^>]*>(.*?)</a>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex TrackerLinkRegex = new(
        @"https?://tracker\.yandex\.ru/([A-Z][\w-]+-\d+)",
        RegexOptions.Compiled);

    private static readonly Regex FigmaLinkRegex = new(
        @"https?://www\.figma\.com/[^\s""<>]+",
        RegexOptions.Compiled);

    private static readonly Regex GoogleDocsLinkRegex = new(
        @"https?://docs\.google\.com/[^\s""<>]+",
        RegexOptions.Compiled);

    private static readonly Regex WikiLinkRegex = new(
        @"https?://wiki\.yandex\.ru/[^\s""<>]+",
        RegexOptions.Compiled);

    /// <summary>
    /// Результат обработки HTML: чистый текст + извлечённые картинки.
    /// </summary>
    public record ProcessedHtml(string Text, List<ExtractedImage> Images);

    public record ExtractedImage(string FileName, byte[] Data);

    /// <summary>
    /// Обработать HTML-контент: извлечь картинки, очистить теги.
    /// </summary>
    public static ProcessedHtml Process(string? html, string prefix, ref int imageCounter)
    {
        if (string.IsNullOrEmpty(html))
            return new ProcessedHtml(string.Empty, new List<ExtractedImage>());

        var images = new List<ExtractedImage>();
        var text = html;
        var counter = imageCounter;

        // 1. Извлекаем base64 картинки
        text = Base64ImageRegex.Replace(text, match =>
        {
            var ext = match.Groups[1].Value;
            var b64 = match.Groups[2].Value;
            counter++;
            var fileName = $"{prefix}_{counter}.{ext}";

            try
            {
                var data = Convert.FromBase64String(b64.Replace("\n", "").Replace("\r", "").Replace(" ", ""));
                images.Add(new ExtractedImage(fileName, data));
                return $"[Изображение: {fileName}]";
            }
            catch
            {
                return "[Изображение: ошибка]";
            }
        });

        // 2. Ссылки → текст
        text = LinkRegex.Replace(text, match =>
        {
            var href = match.Groups[1].Value;
            var linkText = Regex.Replace(match.Groups[2].Value, @"<[^>]+>", "").Trim();
            return string.IsNullOrEmpty(linkText) || linkText == href ? href : $"{linkText} ({href})";
        });

        // 3. HTML → переносы строк
        text = Regex.Replace(text, @"</p>\s*<p>", "\n");
        text = Regex.Replace(text, @"<br\s*/?>", "\n");
        text = Regex.Replace(text, @"</?(p|div|li|ul|ol)\s*>", "\n");
        text = Regex.Replace(text, @"<[^>]+>", "");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = text.Trim();

        imageCounter = counter;
        return new ProcessedHtml(text, images);
    }

    /// <summary>
    /// Извлечь ссылки на трекеры из текста.
    /// </summary>
    public static List<global::Models.Link> ExtractLinks(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<global::Models.Link>();

        var seen = new HashSet<string>();
        var links = new List<global::Models.Link>();

        // Yandex Tracker → Issue
        foreach (Match m in TrackerLinkRegex.Matches(text))
        {
            var url = m.Value.TrimEnd(')');
            if (seen.Add(url))
                links.Add(new global::Models.Link { Url = url, Title = m.Groups[1].Value, Type = global::Models.LinkType.Issue });
        }

        // Figma → Related
        foreach (Match m in FigmaLinkRegex.Matches(text))
        {
            var url = m.Value.TrimEnd(')');
            if (seen.Add(url))
                links.Add(new global::Models.Link { Url = url, Title = "Figma", Type = global::Models.LinkType.Related });
        }

        // Google Docs → Related
        foreach (Match m in GoogleDocsLinkRegex.Matches(text))
        {
            var url = m.Value.TrimEnd(')');
            if (seen.Add(url))
                links.Add(new global::Models.Link { Url = url, Title = "Google Docs", Type = global::Models.LinkType.Related });
        }

        // Wiki → Related
        foreach (Match m in WikiLinkRegex.Matches(text))
        {
            var url = m.Value.TrimEnd(')');
            if (seen.Add(url))
                links.Add(new global::Models.Link { Url = url, Title = "Wiki", Type = global::Models.LinkType.Related });
        }

        return links;
    }
}
