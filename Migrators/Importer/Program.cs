using Importer.Client;
using Importer.Client.Extensions;
using Importer.Client.Implementations;
using Importer.Extensions;
using Importer.Services;
using Importer.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;

namespace Importer;

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
            .UseSerilog((context, services, configuration) =>
                SerilogAction(context, services, configuration, options)
            )
            .ConfigureServices((builder, services) =>
            {
                services.AddSingleton<IImportService, ImportService>();
                services.AddSingleton<App>();
                services.AddSingleton(SetupConfiguration());
                services.RegisterAppConfig();
                
                services.RegisterApiServices();
                services.RegisterClient();
                
                services.AddSingleton<IAdapterHelper, AdapterHelper>();
                services.AddSingleton<IClientAdapter, ClientAdapter>();
                services.AddSingleton<IParserService, ParserService>();
                services.AddSingleton<IAttributeService, AttributeService>();
                services.AddSingleton<IParameterService, ParameterService>();
                services.AddSingleton<ISectionService, SectionService>();
                services.AddSingleton<IBaseWorkItemService, BaseWorkItemService>();
                services.AddSingleton<ISharedStepService, SharedStepService>();
                services.AddSingleton<ITestCaseService, TestCaseService>();
                services.AddSingleton<IAttachmentService, AttachmentService>();
                services.AddSingleton<IProjectService, ProjectService>();
                services.AddSingleton<ITestCaseImportErrorLogService, TestCaseImportErrorLogService>();
            });
    }

    private static void SerilogAction(HostBuilderContext context,
        IServiceProvider services,
        LoggerConfiguration configuration,
        ConfigurationReaderOptions options)
    {
        configuration
            .ReadFrom.Configuration(context.Configuration, options)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/import-log.txt",
                LogEventLevel.Debug,
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 100,
                fileSizeLimitBytes: 209715200,
                rollOnFileSizeLimit: true
            )
            .WriteTo.Console(LogEventLevel.Information);
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