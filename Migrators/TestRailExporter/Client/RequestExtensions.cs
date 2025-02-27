namespace TestRailExporter.Client;

public static class RequestExtensions
{
    public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request)
    {
        HttpContent? newContent = null;
        if (request.Content != null)
        {
            newContent = await request.Content.CloneAsync().ConfigureAwait(false);
        }

        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = newContent,
            Version = request.Version
        };
        foreach (var opt in request.Options)
        {
            switch (opt.Value)
            {
                case string s:
                    clone.Options.Set(new HttpRequestOptionsKey<string>(opt.Key), s);
                    break;
                default:
                    throw new InvalidOperationException("Can't deal with non-string message options ... yet.");
            }
        }
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private static async Task<HttpContent?> CloneAsync(this HttpContent? content)
    {
        if (content == null) return null;

        var ms = new MemoryStream();
        await content.CopyToAsync(ms).ConfigureAwait(false);
        ms.Position = 0;

        var clone = new StreamContent(ms);
        foreach (var header in content.Headers)
        {
            clone.Headers.Add(header.Key, header.Value);
        }
        return clone;
    }
}
