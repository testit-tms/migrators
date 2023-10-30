using System.Text.Json.Serialization;

namespace XRayExporter.Models;

public class XRayTests
{
    [JsonPropertyName("tests")]
    public List<XRayTest> Tests { get; set; }
}

public class XRayTest
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }
}

public class XRayTestFull : XRayTest
{
    [JsonPropertyName("self")]
    public string Self { get; set; }

    [JsonPropertyName("reporter")]
    public string Reporter { get; set; }

    [JsonPropertyName("precondition")]
    public List<Precondition> Preconditions { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("definition")]
    public Definition Definition { get; set; }
}

public class Precondition
{
    [JsonPropertyName("condition")]
    public string Condition { get; set; }
}

public class Definition
{
    [JsonPropertyName("steps")]
    public List<Steps> Steps { get; set; }
}

public class Steps
{
    [JsonPropertyName("step")]
    public Step Step { get; set; }

    [JsonPropertyName("data")]
    public Data Data { get; set; }

    [JsonPropertyName("result")]
    public Result Result { get; set; }

    [JsonPropertyName("attachments")]
    public List<XRayAttachments> Attachments { get; set; }
}

public class Step
{
    [JsonPropertyName("rendered")]
    public string Rendered { get; set; }
}

public class Data
{
    [JsonPropertyName("rendered")]
    public string Rendered { get; set; }
}

public class Result
{
    [JsonPropertyName("rendered")]
    public string Rendered { get; set; }
}

public class XRayAttachments
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    [JsonPropertyName("fileURL")]
    public string FileURL { get; set; }
}

