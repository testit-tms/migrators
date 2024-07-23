using Importer.Client;
using Importer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;

namespace Importer
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
                    .WriteTo.File("logs/import-log.txt",
                        restrictedToMinimumLevel: LogEventLevel.Debug,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                    )
                    .WriteTo.Console(LogEventLevel.Information)
                )
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<IImportService, ImportService>();
                    services.AddSingleton<App>();
                    services.AddSingleton(SetupConfiguration());
                    services.AddSingleton<IParserService, ParserService>();
                    services.AddSingleton<IClient, Client.Client>();
                    services.AddSingleton<IAttributeService, AttributeService>();
                    services.AddSingleton<IParameterService, ParameterService>();
                    services.AddSingleton<ISectionService, SectionService>();
                    services.AddSingleton<ISharedStepService, SharedStepService>();
                    services.AddSingleton<ITestCaseService, TestCaseService>();
                    services.AddSingleton<IAttachmentService, AttachmentService>();
                    services.AddSingleton<IProjectService, ProjectService>();
                });
        }

        private static IConfiguration SetupConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("tms.config.json")
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
