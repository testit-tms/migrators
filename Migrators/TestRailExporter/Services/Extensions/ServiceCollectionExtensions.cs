using JsonWriter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestRailExporter.Models.Client;
using TestRailExporter.Services.Implementations;
using ZephyrScaleServerExporter.Validators;

namespace TestRailExporter.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
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

}
