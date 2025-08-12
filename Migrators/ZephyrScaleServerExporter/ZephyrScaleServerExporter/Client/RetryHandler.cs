using Microsoft.Extensions.Logging;

namespace ZephyrScaleServerExporter.Client;

public class RetryHandler(ILogger<RetryHandler> logger, int maxRetries = 3, TimeSpan? delay = null)
    : DelegatingHandler
{
    private readonly TimeSpan _delay = delay ?? TimeSpan.FromMilliseconds(100);


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int attempt = 0;

        while (true)
        {
            try
            {
                var requestCopy = request;
                if (attempt > 0)
                {
                    requestCopy = await request.CloneAsync();
                }
                var response = await base.SendAsync(requestCopy, cancellationToken);

                if (response.IsSuccessStatusCode || attempt >= maxRetries)
                    return response;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                logger.LogError(ex, "Error during HTTP request on attempt {Attempt}, retrying...", attempt + 1);
            }

            attempt++;
            logger.LogWarning("Retrying request to {Url}, attempt {Attempt}", request.RequestUri, attempt);
            await Task.Delay(_delay, cancellationToken); 
        }
    }
}
