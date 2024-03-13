using JsonWriter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;
using System.Xml.Serialization;
using TestRailExporter.Models;
using TestRailExporter.Services;

namespace TestRailExporter;

internal class Program
{
    static async Task Main()
    {
        using var host = CreateHostBuilder().Build();
        await using var scope = host.Services.CreateAsyncScope();

        try
        {
            await scope.ServiceProvider.GetRequiredService<App>().RunAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
        }
    }

    private static IHostBuilder CreateHostBuilder()
    {
        var options = new ConfigurationReaderOptions(typeof(ConsoleLoggerConfigurationExtensions).Assembly,
            typeof(SerilogExpression).Assembly);

        return Host.CreateDefaultBuilder()
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration, options)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/log.txt",
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(LogEventLevel.Information))
            .ConfigureServices((_, services) =>
            {
                services.AddScoped<App>();
                services.AddSingleton(SetupConfiguration());
                services.AddScoped(provider => new XmlSerializer(typeof(TestRailsXmlSuite)));
                services.AddScoped<IWriteService, WriteService>();
                services.AddScoped<ImportService>();
                services.AddScoped<ExportService>();
            });
    }

    private static IConfiguration SetupConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testrail.config.json")
            .AddEnvironmentVariables()
            .Build();
    }
}
