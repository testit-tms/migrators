using Importer.Client.Implementations;
using Microsoft.Extensions.DependencyInjection;
using TestIT.ApiClient.Api;

namespace Importer.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterApiServices(this IServiceCollection services)
    {
        services.AddTransient<IApiConfigurationFactory, ApiConfigurationFactory>();

        services.AddTransient<IAttachmentsApi>(ApiClientFactory<AttachmentsApi>);
        services.AddTransient<IProjectsApi>(ApiClientFactory<ProjectsApi>);
        services.AddTransient<IProjectAttributesApi>(ApiClientFactory<ProjectAttributesApi>);
        services.AddTransient<IProjectSectionsApi>(ApiClientFactory<ProjectSectionsApi>);
        services.AddTransient<ISectionsApi>(ApiClientFactory<SectionsApi>);
        services.AddTransient<ICustomAttributesApi>(ApiClientFactory<CustomAttributesApi>);
        services.AddTransient<IWorkItemsApi>(ApiClientFactory<WorkItemsApi>);
        services.AddTransient<IParametersApi>(ApiClientFactory<ParametersApi>);
    }

    private static T ApiClientFactory<T>(IServiceProvider sp) where T : class
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("ClientApi");

        var configFactory = sp.GetRequiredService<IApiConfigurationFactory>();
        var config = configFactory.Create();

        return Activator.CreateInstance(typeof(T), client, config, null) as T
               ?? throw new InvalidOperationException($"Cannot create instance of {typeof(T)}");
    }
}