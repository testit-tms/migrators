namespace Importer.Client;

internal class HttpClientFactory
{
    public static HttpClient Create(HttpMessageHandler handler, TimeSpan timeout)
    {
        var client = new HttpClient(handler);
        client.Timeout = timeout;
        return client;
    }
}
