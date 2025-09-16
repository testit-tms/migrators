using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        var ver = "0.2.1";
        logger.LogInformation("version: {Version} ; OS - {Os} : {OsVer} \n logical core count: {Count}",
            ver, os, osVersion, Utils.GetLogicalProcessors());

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
            // TODO: fix
            // exportService.ExportProject().Wait();
            exportService.ExportProjectCloud().Wait();
        }



        logger.LogInformation("Ending application");
    }
}
