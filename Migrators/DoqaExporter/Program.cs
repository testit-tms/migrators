using DoqaExporter.Client;
using DoqaExporter.Models;
using DoqaExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace DoqaExporter;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/doqa-export-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("doqa.config.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var doqaConfig = new DoqaConfig();
            configuration.GetSection("doqa").Bind(doqaConfig);

            if (string.IsNullOrEmpty(configuration["resultPath"]))
                throw new ArgumentException("resultPath is not set in doqa.config.json");
            if (string.IsNullOrEmpty(doqaConfig.Url))
                throw new ArgumentException("doqa.url is not set in doqa.config.json");
            if (string.IsNullOrEmpty(doqaConfig.Email) || string.IsNullOrEmpty(doqaConfig.Password))
                throw new ArgumentException("doqa.email and doqa.password are required in doqa.config.json");

            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddSerilog(dispose: true));
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(doqaConfig);
            services.AddSingleton<IWriteService, WriteService>();

            services.AddHttpClient<IDoqaClient, Client.DoqaClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            services.AddTransient<IExportService, ExportService>();

            var provider = services.BuildServiceProvider();
            var exportService = provider.GetRequiredService<IExportService>();

            await exportService.ExportProject();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error");
            Environment.Exit(1);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
