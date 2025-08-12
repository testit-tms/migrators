namespace ZephyrScaleServerExporter.Services;

public interface IDetailedLogService
{
    public void LogDebug(string? message, params object?[] args);

    public void LogInformation(string? message, params object?[] args);
}