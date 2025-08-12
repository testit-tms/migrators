using Microsoft.Extensions.DependencyInjection;
using ZephyrScaleServerExporter.BatchMerging.Extensions;
using ZephyrScaleServerExporter.Services.Implementations;
using ZephyrScaleServerExporter.Services.TestCase.Extensions;

namespace ZephyrScaleServerExporter.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IFolderService, FolderService>();
        services.AddSingleton<IStepService, StepService>();
        services.AddSingleton<IAttributeService, AttributeService>();
        services.AddSingleton<IWriteService, WriteService>();
        services.AddSingleton<IAttachmentService, AttachmentService>();
        services.AddSingleton<IParameterService, ParameterService>();
        services.AddSingleton<IStatusService, StatusService>();
        
        services.AddTestCaseServices();
        services.AddSingleton<ITestCaseBatchService, TestCaseBatchService>();
        services.AddSingleton<ITestCaseService, TestCaseService>();
        services.AddSingleton<ITestCaseErrorLogService, TestCaseErrorLogService>();

        
        services.AddBatchMerging();
    }
    
}