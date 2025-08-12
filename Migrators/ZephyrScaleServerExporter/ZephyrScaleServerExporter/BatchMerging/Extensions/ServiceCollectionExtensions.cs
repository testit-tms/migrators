using Microsoft.Extensions.DependencyInjection;
using ZephyrScaleServerExporter.BatchMerging.Implementations;

namespace ZephyrScaleServerExporter.BatchMerging.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddBatchMerging(this IServiceCollection services)
    {
        services.AddSingleton<IMergeProcessor, MergeProcessor>();
        services.AddSingleton<IFileProcessor, FileProcessor>();
        services.AddSingleton<IMainJsonProcessor, MainJsonProcessor>();
    }
    
}