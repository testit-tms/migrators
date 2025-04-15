using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestRailExporter.Models.Client;

public class TestRailAttachment
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(IdConverter))]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cassandra_file_id")]
    public string Guid { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;
}

class IdConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt64().ToString();
            case JsonTokenType.String:
                return reader.GetString()!;
            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

public class TestRailAttachments : TastRailBaseEntity
{
    [JsonPropertyName("attachments")]
    public List<TestRailAttachment> Attachments { get; set; } = new();
}
