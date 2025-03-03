using Importer.Models;
using Importer.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Importer.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterAppConfig(this IServiceCollection services)
    {
        services
            .AddOptions<AppConfig>()
            .BindConfiguration("")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AppConfig>, AppConfigValidator>();

        using var sp = services.BuildServiceProvider();
        var config = sp.GetRequiredService<IOptions<AppConfig>>();
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("[LOG] Selected timeout: " + config.Value.Tms.Timeout + " seconds");
    }

    public static void RegisterClient(this IServiceCollection services)
    {
        services.AddHttpClient("ClientApi", (sp, client) =>
            {
                var config = sp.GetRequiredService<IOptions<AppConfig>>();
                client.Timeout = TimeSpan.FromSeconds(config.Value.Tms.Timeout);
            })
            .AddPolicyHandler((provider, request) =>
                // GET requests only policy    
                request.Method == HttpMethod.Get
                    ? provider.GetRetryPolicy()
                    : Policy.NoOpAsync<HttpResponseMessage>())
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
                };

                var config = sp.GetRequiredService<IOptions<AppConfig>>();
                var certValidation = config.Value.Tms.CertValidation;
                if (!certValidation)
                    handler.SslOptions.RemoteCertificateValidationCallback =
                        (_, _, _, _) => true;

                return handler;
            });
    }
}