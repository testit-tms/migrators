using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Xml.Serialization;
using TestRailImporter.Models;
using TestRailImporter.Services;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;

namespace TestRailImporter;

internal class Program
{
    static async Task Main(string[] args)
    {
        // For local debug only
        args = new string[]
        {
            "C:\\Users\\petr.komissarov\\example.xml"
        };

        using var host = CreateHostBuilder().Build();
        await using var scope = host.Services.CreateAsyncScope();

        try
        {
            await scope.ServiceProvider.GetRequiredService<App>().RunAsync(args).ConfigureAwait(false);
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
                services.AddScoped<TestRailReader>();
            });
    }

    private static IConfiguration SetupConfiguration()
    {
        var jsonConfig = Directory
            .GetFiles(AppContext.BaseDirectory, "tms.config.json", SearchOption.AllDirectories)
            .Single();

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(jsonConfig)
            .AddEnvironmentVariables()
            .Build();
    }
}
