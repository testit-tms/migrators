namespace AllureExporter.Client;

public interface IClient
{
    Task<int> GetProjectId();
}
