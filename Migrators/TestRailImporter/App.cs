using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;
using System.Text;
using TestRailImporter.Models;
using TestRailImporter.Services;

namespace TestRailImporter;

public class App
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<App> _logger;
    private readonly TestRailReader _testRailReader;
    private readonly TestRailBinder _testRailBinder;

    public App(IConfiguration configuration, ILogger<App> logger, TestRailReader testRailReader,
        TestRailBinder testRailBinder)
    {
        _configuration = configuration;
        _logger = logger;
        _testRailReader = testRailReader;
        _testRailBinder = testRailBinder;
    }

    public async Task RunAsync(string[] args)
    {
        _logger.LogInformation("Starting application");

        var resultPath = _configuration["resultPath"];

        foreach (var filePath in args)
        {
            try
            {
                (var testRailsXmlSuite, var customAttributes) = await ReadXmlAsync(filePath).ConfigureAwait(false);
                var root = _testRailBinder.ConvertTestRailXmlSuite(testRailsXmlSuite, customAttributes);
                await WriteMainJsonAsync(resultPath, root).ConfigureAwait(false);
                Importer.Program.Main(args);

                _logger.LogInformation("Xml file '{filePath}' import success", filePath);
            }
            catch (Exception exception)
            {
                _logger.LogError("Xml file '{filePath}' import failed: {exception}", filePath, exception);
            }
        }

        _logger.LogInformation("Ending application");
    }

    private async Task<(TestRailsXmlSuite testRailsXmlSuite, List<CustomAttributeModel> customAttributes)> ReadXmlAsync(
        string filePath)
    {
        await using var fileStream = File.OpenRead(filePath);
        await using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

        memoryStream.Seek(0, SeekOrigin.Begin);
        var testRailsXmlSuite = _testRailReader.Read(memoryStream);

        memoryStream.Seek(0, SeekOrigin.Begin);
        var customAttributes = await TestRailReader.GetCustomAttributesAsync(memoryStream).ConfigureAwait(false);

        return (testRailsXmlSuite, customAttributes);
    }

    private static async Task WriteMainJsonAsync(string? resultPath, Root root)
    {
        Directory.CreateDirectory(resultPath!);
        var mainJsonPath = Path.Combine(Path.GetFullPath(resultPath!), Constants.MainJson);
        var mainJsonText = JsonConvert.SerializeObject(root, Formatting.Indented);

        await File.WriteAllTextAsync(mainJsonPath, mainJsonText, Encoding.UTF8).ConfigureAwait(false);
    }
}
