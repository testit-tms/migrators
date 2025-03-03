using AllureExporter.Client;
using AllureExporter.Extensions;
using AllureExporter.Services.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;

namespace AllureExporter;

internal class Program
{
    private static void Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();
        using var scope = host.Services.CreateScope();

        var services = scope.ServiceProvider;

        try
        {
            services.GetRequiredService<App>().Run(args);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] strings)
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
                    LogEventLevel.Debug,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.Console(LogEventLevel.Information)
            )
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(SetupConfiguration());
                services.RegisterAppConfig();
                services.AddSingleton<App>();
                services.AddScoped<IClient, Client.Client>();
                services.RegisterServices();
            });
    }

    private static IConfiguration SetupConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("allure.config.json")
            .AddEnvironmentVariables()
            .Build();
    }
}
