using HPALMExporter.Client;
using HPALMExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;

namespace HPALMExporter
{
    internal class Program
    {
        static void Main(string[] args)
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

        static IHostBuilder CreateHostBuilder(string[] strings)
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
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                    )
                    .WriteTo.Console(LogEventLevel.Information)
                )
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<App>();
                    services.AddSingleton(SetupConfiguration());
                    services.AddSingleton<IClient, Client.Client>();
                    services.AddSingleton<ISectionService, SectionService>();
                    services.AddSingleton<IWriteService, WriteService>();
                    services.AddSingleton<IExportService, ExportService>();
                    services.AddSingleton<ITestCaseService, TestCaseService>();
                    services.AddSingleton<IAttachmentService, AttachmentService>();
                    services.AddSingleton<IAttributeService, AttributeService>();
                });
        }

        private static IConfiguration SetupConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("hpalm.config.json")
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
