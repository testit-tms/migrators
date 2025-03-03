using System.Net;
using Importer.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Importer.Extensions;

internal static class ServiceProviderPolicyExtensions
{
    public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(this IServiceProvider provider)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outcome, timespan, retryAttempt, context) =>
                {
                    try
                    {
                        var logger = provider.GetRequiredService<ILogger<IClientAdapter>>();
                        logger.LogWarning(
                            $"Retry {retryAttempt} for GET {outcome?.Result?.RequestMessage?.RequestUri}");
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                });
    }
}