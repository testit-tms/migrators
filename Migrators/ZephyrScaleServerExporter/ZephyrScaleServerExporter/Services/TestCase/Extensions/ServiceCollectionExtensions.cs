using Microsoft.Extensions.DependencyInjection;
using ZephyrScaleServerExporter.Services.TestCase.Helpers;
using ZephyrScaleServerExporter.Services.TestCase.Helpers.Implementations;
using ZephyrScaleServerExporter.Services.TestCase.Implementations;

namespace ZephyrScaleServerExporter.Services.TestCase.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddTestCaseServices(this IServiceCollection services)
    {
        services.AddSingleton<ITestCaseServiceHelper, TestCaseServiceHelper>();
        
        services.AddSingleton<ITestCaseAdditionalLinksService, TestCaseAdditionalLinksService>();
        services.AddSingleton<ITestCaseAttributesService, TestCaseAttributesService>();
        services.AddSingleton<ITestCaseAttachmentsService, TestCaseAttachmentsService>();
        services.AddSingleton<ITestCaseConvertService, TestCaseConvertService>();
        services.AddSingleton<ITestCaseCommonService, TestCaseCommonService>();
    }
}