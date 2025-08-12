using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal sealed class DetailedLogService(
    IOptions<AppConfig> options,
    ILogger<DetailedLogService> logger)
    : IDetailedLogService
{
    public void LogDebug(string? message, params object?[] args)
    {
        if (options.Value?.DetailedLog == true)
        {
            logger.LogDebug(message, args);    
        }
    }
    
    public void LogInformation(string? message, params object?[] args)
    {
        if (options.Value?.DetailedLog == true)
        {
            logger.LogInformation(message, args);    
        }
    }
}