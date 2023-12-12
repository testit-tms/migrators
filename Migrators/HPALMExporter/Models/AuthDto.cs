using System.Text.Json.Serialization;

namespace HPALMExporter.Models;

public class AuthDto
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; }

    [JsonPropertyName("secret")]
    public string Secret { get; set; }
}
