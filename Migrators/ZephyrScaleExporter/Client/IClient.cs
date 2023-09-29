using ZephyrScaleExporter.Models;

namespace ZephyrScaleExporter.Client;

public interface IClient
{
    Task<ZephyrProject> GetProject();
}
