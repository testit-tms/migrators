using System.Text.Json.Serialization;

namespace ZephyrScaleServerExporter.Models.TestCases
{
    // ZephyrTestScript -> ZephyrStep (Steps) -> this (Attachments)
    public class StepAttachment
    {
        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }
        [JsonPropertyName("createdOn")]
        public string? CreatedOn { get; set; }
        [JsonPropertyName("userKey")]
        public string? UserKey { get; set; }
    }
}
