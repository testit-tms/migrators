using ZephyrScaleServerExporter.Models.Common;

namespace ZephyrScaleServerExporter.Services;

public interface IStatusService
{
    Task<StatusData> ConvertStatuses(string projectId);

    Task<StatusData> ConvertStatusesCloud(string projectKey);
}
