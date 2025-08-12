using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ZephyrScaleServerExporter.Models.TestCases
{
    public class TestCaseResponseWrapper
    {
        [JsonPropertyName("results")]
        public required List<ZephyrTestCaseRoot> Results { get; set; }
    }


    public class TestCaseTracesResponseWrapper
    {
        [JsonPropertyName("results")]
        public required List<TraceLinksRoot> Results { get; set; }
    }


    public class ConfluencePageLink
    {
        [JsonPropertyName("appId")]
        public required string AppId { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("pageId")]
        public required string PageId { get; set; }
    }

    public class IssueLink
    {
        [JsonPropertyName("issueId")]
        public required string IssueId { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class Priority
    {
        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("color")]
        public required string Color { get; set; }

        [JsonPropertyName("i18nKey")]
        public string I18nKey { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }
    }

    public class TraceLinksRoot
    {
        [JsonPropertyName("traceLinks")]
        public List<TraceLink>? TraceLinks { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    // 'traceLinks'; 'confluencePageLinks'; 'updatedBy'; 'precondition'.
    // 'objective'; 'customFields'; 'parameters'.
    public class ZephyrTestCaseRootFolder
    {
        [JsonPropertyName("customFieldValues")]
        public List<object>? CustomFieldValues { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("folderType")]
        public required string FolderType { get; set; }
    }

    public class ZephyrTestCaseRoot
    {
        [JsonPropertyName("traceLinks")]
        public List<TraceLink>? TraceLinks { get; set; }

        [JsonPropertyName("confluencePageLinks")]
        public List<ConfluencePageLink>? ConfluencePageLinks { get; set; }

        [JsonPropertyName("updatedBy")]
        public string? UpdatedBy { get; set; }

        [JsonPropertyName("updatedOn")]
        public DateTime UpdatedOn { get; set; }

        [JsonPropertyName("priority")]
        public Priority Priority { get; set; }
        [JsonPropertyName("majorVersion")]

        public int MajorVersion { get; set; }
        [JsonPropertyName("createdOn")]

        public DateTime CreatedOn { get; set; }
        [JsonPropertyName("labels")]

        public List<string>? Labels { get; set; }

        [JsonPropertyName("archived")]
        public bool Archived { get; set; }

        [JsonPropertyName("customFieldValues")]
        public List<object>? CustomFieldValues { get; set; }

        [JsonPropertyName("createdBy")]
        public string? CreatedBy { get; set; }

        [JsonPropertyName("precondition")]
        public string? Precondition { get; set; }

        [JsonPropertyName("folder")]
        public ZephyrTestCaseRootFolder? Folder { get; set; }

        [JsonPropertyName("objective")]
        public string? Description { get; set; }

        [JsonPropertyName("customFields")]
        public Dictionary<string, object>? CustomFields { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object>? Parameters { get; set; }

        [JsonPropertyName("latestVersion")]
        public bool LatestVersion { get; set; }

        [JsonPropertyName("testScript")]
        public required ZephyrTestScript TestScript { get; set; }

        [JsonPropertyName("issueLinks")]
        public List<IssueLink>? IssueLinks { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("key")]
        public required string Key { get; set; }

        [JsonPropertyName("status")]
        public required Status Status { get; set; }

        [JsonPropertyName("component")]
        public string? Component { get; set; }

        [JsonPropertyName("owner")]
        public string? OwnerKey { get; set; }

    }

    public class Status
    {
        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("color")]
        public required string Color { get; set; }

        [JsonPropertyName("i18nKey")]
        public string? I18nKey { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }
    }


    public class TraceLink
    {
        [JsonPropertyName("issueId")]
        public string? IssueId { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public Type? Type { get; set; }

        [JsonPropertyName("urlDescription")]
        public string? UrlDescription { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("confluenceAppId")]
        public string? ConfluenceAppId { get; set; }

        [JsonPropertyName("confluencePageId")]
        public string? ConfluencePageId { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Type
    {
        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("systemKey")]
        public required string SystemKey { get; set; }
    }
}
