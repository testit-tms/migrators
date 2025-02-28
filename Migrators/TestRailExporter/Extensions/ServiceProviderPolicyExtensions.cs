using System.Net;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace TestRailExporter.Extensions;

internal static class ServiceProviderPolicyExtensions
{
    public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(this IServiceProvider provider)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromMinutes(1),
                (outcome, timespan, retryAttempt, context) => {});
    }
}
