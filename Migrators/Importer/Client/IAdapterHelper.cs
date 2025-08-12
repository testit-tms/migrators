namespace Importer.Client;

public interface IAdapterHelper
{
    Task<TResponse?> RetryCaller<TResponse>(Func<Task<TResponse>> action);
}