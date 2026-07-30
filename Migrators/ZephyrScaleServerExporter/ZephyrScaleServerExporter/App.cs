using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using ZephyrScaleServerExporter.BatchMerging;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Helpers;

namespace ZephyrScaleServerExporter;

public class App(
    ILogger<App> logger,
    IOptions<AppConfig> config,
    IMergeProcessor mergeProcessor,
    IExportService exportService)
{
    private static OSPlatform? GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OSPlatform.Linux;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }
        return null;
    }

    public void Run(string[] args)
    {
        var os = GetOperatingSystem();
        var osVersion = Environment.OSVersion;
        logger.LogInformation("Version: {Version} ; OS - {Os} : {OsVer} ; logical core count: {Count}",
            AppVersion.Current, os, osVersion, Utils.GetLogicalProcessors());

        if (config.Value.Zephyr.Merge)
        {
            mergeProcessor.MergeProjects();
        }
        else if (config.Value.Zephyr.Partial)
        {
            exportService.ExportProjectBatch().Wait();
        }
        else
        {
            exportService.ExportProject().Wait();
        }
        
        

        logger.LogInformation("Ending application");
    }
}
