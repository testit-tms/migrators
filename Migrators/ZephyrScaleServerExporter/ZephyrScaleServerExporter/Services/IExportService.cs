namespace ZephyrScaleServerExporter.Services;

public interface IExportService
{
    Task ExportProject();

    Task ExportProjectBatch();

    Task ExportProjectCloud();
}
