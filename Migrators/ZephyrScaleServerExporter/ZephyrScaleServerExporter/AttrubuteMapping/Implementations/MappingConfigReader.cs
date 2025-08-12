using System.Text.Json;
using ZephyrScaleServerExporter.AttrubuteMapping.Models;

namespace ZephyrScaleServerExporter.AttrubuteMapping.Implementations;


internal class MappingConfigReader: IMappingConfigReader
{
    // Ключ — это значение "AttributeName:Value, например Авторизован:Определенно".
    // Значение — это пара (AttributeName, MappedValue) (например, ("Авторизован", "Да")).
    private Dictionary<string, (string AttributeName, string MappedValue)> _reverseMappings = new();
    private bool _isInit;


    public void InitOnce(string configFilePath, string mappingFilesDirectory)
    {
        if (_isInit)
        {
            return;
        }
        _isInit = true;
        
        var config = ReadConfig(configFilePath);
        
        _reverseMappings = new(StringComparer.OrdinalIgnoreCase);

        foreach (var mappingBlock in config.Mappings)
        {
            foreach (var valueBlock in mappingBlock.Values)
            {
                var mappingValues = ReadFileContent(valueBlock, mappingFilesDirectory);
                FillMappingWithValuePairs(mappingBlock, valueBlock, mappingValues);
            }
        }
    }

    private static MappingConfig ReadConfig(string configFilePath)
    {
        string json;
        try
        {
            json = File.ReadAllText(configFilePath);
        }
        catch (Exception e)
        {
            throw new FileLoadException("Could not load configuration file: " + configFilePath, e);
        }
        
        var config = JsonSerializer.Deserialize<MappingConfig>(json)
                     ?? throw new InvalidOperationException("Config file is empty or invalid.");

        return config;
    }

    private static List<string> ReadFileContent(MappingValue valueBlock, string mappingFilesDirectory)
    {
        var filePath = Path.Combine(mappingFilesDirectory, valueBlock.MappingFile);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Mapping file '{filePath}' not found.");
        }

        var mappedValues = File.ReadAllLines(filePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return mappedValues;
    }

    private void FillMappingWithValuePairs(Mapping mappingBlock, MappingValue valueBlock, IEnumerable<string> mappingValues)
    {
        foreach (var mappedValue in mappingValues)
        {
            if (!_reverseMappings.TryGetValue(mappedValue, out var mapping))
            {
                _reverseMappings[mappingBlock.Name+":"+mappedValue] = (mappingBlock.Name, valueBlock.Value);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Duplicate value '{mappedValue}' found in mappings for keys " +
                    $"'{mapping.AttributeName}' and '{mappingBlock.Name}'.");
            }
        }
    }
    
    public string? GetMappingForValue(string targetValue, string attributeName)
    {
        if (_reverseMappings.Count == 0)
        {
            return null;
        }
        if (_reverseMappings.TryGetValue(attributeName + ":" + targetValue, out var mapping))
        {
            return mapping.MappedValue;
        }
        return null;
    }
}