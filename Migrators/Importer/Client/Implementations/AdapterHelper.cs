using Microsoft.Extensions.Logging;

namespace Importer.Client.Implementations;

internal class AdapterHelper(ILogger<ClientAdapter> logger): IAdapterHelper
{
    private bool ContainsNetworkError(Exception ex)
    {
        List<string> networkErrorMarkers = ["HttpRequestException", 
            "An error occured while sending", "InfluxClientException",
            "SocketClientException", "InternalServerError",
            "Error while checking a license", "LicenseCheckFailedException",
            "The response ended", "ResponseEnded"
        ];

        if (ex.InnerException != null)
        {
            var isAny = networkErrorMarkers.Any(marker => ex.InnerException.Message.Contains(marker));
            if (isAny)
            {
                return true;
            }
        }

        return networkErrorMarkers.Any(marker => ex.Message.Contains(marker));
    }

    public async Task<TResponse?> RetryCaller<TResponse>(Func<Task<TResponse>> action)
    {
        TResponse? response = default;
        Exception? lastException = null;
        var retry = 0;
        var retries = 8;
        var delay = TimeSpan.FromSeconds(2);
        while (retry < retries)
        {
            retry++;
            try
            {
                response = await action();
                lastException = null;
                break;
            }
            catch (Exception e)
            {
                if (!ContainsNetworkError(e)) throw;
                lastException = e;
                logger.LogError("Could not perform request with attempt №{A}, {Message}",
                    retry,
                    e.Message);
                if (retry < retries)
                {
                    await Task.Delay(delay);
                }
            }
        }

        if (lastException != null)
        {
            throw lastException;
        }
        return response;
    }
}