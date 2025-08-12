using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Validators;

namespace ZephyrScaleServerExporter.Models.Extensions;

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
    }
}