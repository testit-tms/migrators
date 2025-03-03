using Importer.Client.Implementations;
using Microsoft.Extensions.DependencyInjection;
using TestIT.ApiClient.Api;

namespace Importer.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterApiServices(this IServiceCollection services)
    {
        services.AddTransient<IApiConfigurationFactory, ApiConfigurationFactory>();

        services.AddTransient(ApiClientFactory<AttachmentsApi>);
        services.AddTransient(ApiClientFactory<ProjectsApi>);
        services.AddTransient(ApiClientFactory<ProjectAttributesApi>);
        services.AddTransient(ApiClientFactory<ProjectSectionsApi>);
        services.AddTransient(ApiClientFactory<SectionsApi>);
        services.AddTransient(ApiClientFactory<CustomAttributesApi>);
        services.AddTransient(ApiClientFactory<WorkItemsApi>);
        services.AddTransient(ApiClientFactory<ParametersApi>);
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