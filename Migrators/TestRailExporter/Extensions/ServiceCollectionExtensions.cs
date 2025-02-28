using JsonWriter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using TestRailExporter.Models.Client;
using TestRailExporter.Client;
using TestRailExporter.Services;
using TestRailExporter.Services.Implementations;
using ZephyrScaleServerExporter.Validators;

namespace TestRailExporter.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddTransient<IClient, Client.Client>();
        services.AddSingleton<IAttachmentService, AttachmentService>();
        services.AddSingleton<IWriteService, WriteService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<ISectionService, SectionService>();
        services.AddSingleton<ISharedStepService, SharedStepService>();
        services.AddSingleton<IStepService, StepService>();
        services.AddSingleton<ITestCaseService, TestCaseService>();
    }

    public static void RegisterAppConfig(this IServiceCollection services)
    {
        services
            .AddOptions<AppConfig>()
            .BindConfiguration("")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AppConfig>, AppConfigValidator>();
    }

    public static void RegisterClient(this IServiceCollection services)
    {
        services.AddHttpClient("ClientApi", (sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<AppConfig>>();
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

                return handler;
            });

    }
}
