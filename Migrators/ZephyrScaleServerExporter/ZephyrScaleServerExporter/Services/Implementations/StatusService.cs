using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Common;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal class StatusService(
    IDetailedLogService detailedLogService,
    ILogger<StatusService> logger,
    IOptions<AppConfig> config,
    IClient client) : IStatusService
{
    private readonly List<string> _ignoredStatuses = [];

    public async Task<StatusData> ConvertStatuses(string projectId)
    {
        logger.LogInformation("Converting statuses");

        var statuses = await client.GetStatuses(projectId);
        var stringStatuses = "";
        var stringStatusesBuilder = new StringBuilder();
        var statusAttribute = new Attribute()
        {
            Id = Guid.NewGuid(),
            Name = Constants.ZephyrStatusAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = statuses.Select(x => x.Name).ToList()
        };

        foreach (var status in statuses)
        {
            // check config value for filter ignorance
            // if not ignored -> proceed filtering
            if (!config.Value.Zephyr.IgnoreFilter
                && _ignoredStatuses.Contains(status.Name))
            {
                continue;
            }

            stringStatusesBuilder.Append("\"" + status.Name + "\",");
        }
        stringStatuses = stringStatusesBuilder.ToString().TrimEnd(',');

        detailedLogService.LogInformation("Converted statuses \"{StringStatuses}\"", stringStatuses);

        return new StatusData
        {
            StringStatuses = stringStatuses,
            StatusAttribute = statusAttribute
        };
    }

    public async Task<StatusData> ConvertStatusesCloud(string projectKey)
    {
        logger.LogInformation("Converting statuses");

        var statuses = await client.GetStatusesCloud(projectKey);
        var stringStatuses = "";
        var stringStatusesBuilder = new StringBuilder();
        var statusAttribute = new Attribute()
        {
            Id = Guid.NewGuid(),
            Name = Constants.ZephyrStatusAttribute,
            Type = AttributeType.Options,
            IsRequired = false,
            IsActive = true,
            Options = statuses.Select(x => x.Name).ToList()
        };

        foreach (var status in statuses)
        {
            // check config value for filter ignorance
            // if not ignored -> proceed filtering
            if (!config.Value.Zephyr.IgnoreFilter
                && _ignoredStatuses.Contains(status.Name))
            {
                continue;
            }

            stringStatusesBuilder.Append("\"" + status.Name + "\",");
        }
        stringStatuses = stringStatusesBuilder.ToString().TrimEnd(',');

        detailedLogService.LogInformation("Converted statuses \"{StringStatuses}\"", stringStatuses);

        return new StatusData
        {
            StringStatuses = stringStatuses,
            StatusAttribute = statusAttribute
        };
    }
}
