using TestLinkExporter.Models.Config;
using TestLinkExporter.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TestLinkExporter.Extensions;

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
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("[LOG] Successfully registered app config.");
    }
}
