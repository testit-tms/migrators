using TestLinkExporter.Services.Implementations;
using JsonWriter;
using Microsoft.Extensions.DependencyInjection;

namespace TestLinkExporter.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<IWriteService, WriteService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<ISectionService, SectionService>();
        services.AddSingleton<ITestCaseService, TestCaseService>();
        services.AddSingleton<IStepService, StepService>();
        services.AddSingleton<IAttachmentService, AttachmentService>();
        services.AddSingleton<IAttributeService, AttributeService>();
    }
}
