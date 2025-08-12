using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.AttrubuteMapping.Models;


public class MappingValue
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!; // "Да"
    
    [JsonPropertyName("mappingFile")]
    public string MappingFile { get; set; } = null!; // Путь к файлу с маппингом
}

public class Mapping
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!; // Ключ атрибута (например, "Авторизован")
    
    [JsonPropertyName("values")]
    public List<MappingValue> Values { get; set; } = new(); 
}

public class MappingConfig
{

    [JsonPropertyName("mappings")]
    public List<Mapping> Mappings { get; set; } = new(); 
}