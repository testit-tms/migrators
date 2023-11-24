using System.Xml.Linq;
using System.Xml.Serialization;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;

namespace AzureExporter.Services;

public class ParameterService : IParameterService
{
    private readonly ILogger<ParameterService> _logger;

    public ParameterService(ILogger<ParameterService> logger)
    {
        _logger = logger;
    }

    public List<Dictionary<string, string>> ConvertParameters(AzureParameters parameters)
    {
        _logger.LogInformation("Converting parameters");
        _logger.LogDebug("Parameters: {@Parameters}", parameters);

        if (string.IsNullOrWhiteSpace(parameters.Keys) || !parameters.Keys.Contains("<parameters>"))
        {
            _logger.LogDebug("No keys found in parameters");

            return new List<Dictionary<string, string>>();
        }

        if (!string.IsNullOrWhiteSpace(parameters.Values))
        {
            _logger.LogDebug("Found values in parameters");

            return ParseParameterValues(parameters.Values);
        }

        var keys = ParseParameterKeys(parameters.Keys);

        return new List<Dictionary<string, string>>
        {
            keys.ToDictionary(k => k, v => "Empty")
        };
    }


    private static List<string> ParseParameterKeys(string content)
    {
        using TextReader reader = new StringReader(content);
        var serializer = new XmlSerializer(typeof(AzureParameterKeys));
        var keys = (AzureParameterKeys)serializer.Deserialize(reader)!;

        return keys.Keys.Select(key => key.Name).ToList();
    }

    private static List<Dictionary<string, string>> ParseParameterValues(string content)
    {
        try
        {
            var xmlDoc = XDocument.Parse(content);
            var parameters = xmlDoc.Descendants("Table1")
                .Select(table1Node => table1Node.Elements()
                    .ToDictionary(element => element.Name.LocalName,
                        element => string.IsNullOrWhiteSpace(element.Value) ? "Empty" : element.Value.Trim()))
                .ToList();

            return parameters;
        }
        catch (Exception)
        {
            return new List<Dictionary<string, string>>();
        }
    }
}
