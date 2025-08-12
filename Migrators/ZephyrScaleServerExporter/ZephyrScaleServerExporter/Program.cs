using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Expressions;
using Serilog.Settings.Configuration;
using ZephyrScaleServerExporter.AttrubuteMapping;
using ZephyrScaleServerExporter.AttrubuteMapping.Implementations;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Extensions;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Extensions;
using ZephyrScaleServerExporter.Services.Implementations;

namespace ZephyrScaleServerExporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MappingConfigReader mappingConfigReader = new MappingConfigReader();
                mappingConfigReader.InitOnce("mapping.json", "");
            }
            // allowed to not have "mapping.json"
            // disallowed to have mapping json with incorrect configuration
            catch (FileLoadException ex)
            {
                Console.WriteLine("File not found: mapping.json file" + ex.Message);
            }
           
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
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        static IHostBuilder CreateHostBuilder(string[] strings)
        {
            var options = new ConfigurationReaderOptions(typeof(ConsoleLoggerConfigurationExtensions).Assembly,
                typeof(SerilogExpression).Assembly);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HH_mm_ss");
            string fileName = $"export-log-{timestamp}.txt";
            return Host.CreateDefaultBuilder()
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration, options)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .MinimumLevel.Debug()
                    .Filter.ByExcluding("SourceContext like '%System.Net.Http.HttpClient%' and @l = 'Information'")
                    .WriteTo.File($"logs/{fileName}",
                        restrictedToMinimumLevel: LogEventLevel.Debug,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                        retainedFileCountLimit: 100,
                        fileSizeLimitBytes: 209715200,
                        rollOnFileSizeLimit: true
                    )
                    .WriteTo.Console(LogEventLevel.Information)
                )
                .ConfigureServices((_, services) =>
                {
                    services.RegisterAppConfig();
                    
                    services.AddHttpClient("DefaultHttpClient")
                        .AddHttpMessageHandler(serviceProvider =>
                        {
                            var logger = serviceProvider.GetRequiredService<ILogger<RetryHandler>>();
                            return new RetryHandler(logger, maxRetries: 3, delay: TimeSpan.FromMilliseconds(100));
                        });
                    services.AddHttpClient("ConfluenceHttpClient")
                        .AddHttpMessageHandler(serviceProvider =>
                        {
                            var logger = serviceProvider.GetRequiredService<ILogger<RetryHandler>>();
                            return new RetryHandler(logger, maxRetries: 3, delay: TimeSpan.FromMilliseconds(100));
                        });
                    
                    services.AddSingleton<App>();
                    services.AddSingleton<IDetailedLogService, DetailedLogService>();
                    services.AddScoped<IMappingConfigReader, MappingConfigReader>();
                    services.AddSingleton(SetupConfiguration());
                    services.AddTransient<ITestCaseClient, TestCaseClient>();
                    services.AddTransient<IClient, Client.Client>(serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<Client.Client>>();
                        var config = serviceProvider.GetRequiredService<IOptions<AppConfig>>();
                        var testCaseClient = serviceProvider.GetRequiredService<ITestCaseClient>();
                        var detailedLogService = serviceProvider.GetRequiredService<IDetailedLogService>();

                        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

                        var httpClient = httpClientFactory.CreateClient("DefaultHttpClient");
                        var confluenceHttpClient = httpClientFactory.CreateClient("ConfluenceHttpClient");

                        return new Client.Client(logger, httpClient, config, 
                            testCaseClient, confluenceHttpClient, detailedLogService);
                    });
                    
                    services.AddServices();
                });
        }
        
        

        private static IConfiguration SetupConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("zephyr.config.json")
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
