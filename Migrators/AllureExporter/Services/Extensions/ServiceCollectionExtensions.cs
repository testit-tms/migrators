using AllureExporter.Helpers;
using AllureExporter.Services.Implementations;
using JsonWriter;
using Microsoft.Extensions.DependencyInjection;

namespace AllureExporter.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IWriteService, WriteService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ISharedStepService, SharedStepService>();
        services.AddScoped<ITestCaseService, TestCaseService>();
        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<IStepService, StepService>();
        services.AddScoped<ICoreHelper, CoreHelper>();
    }
}
