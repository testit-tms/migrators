using System.Text.Json.Serialization;

namespace Models;

public class Step
{
    [JsonPropertyName("sharedStepId")]
    public Guid? SharedStepId { get; set; }

    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("expected")]
    public required string Expected { get; set; }

    [JsonPropertyName("actionAttachments")]
    public required List<string> ActionAttachments { get; set; }

    [JsonPropertyName("expectedAttachments")]
    public required List<string> ExpectedAttachments { get; set; }

    [JsonPropertyName("testDataAttachments")]
    public required List<string> TestDataAttachments { get; set; }

    [JsonPropertyName("attachments")]
    public List<string>? Attachments { get; set; }

    [JsonPropertyName("testData")]
    public string? TestData { get; set; }

    public List<string> GetAllAttachments()
    {
        var result = new List<string>();
        result.AddRange(ActionAttachments);
        result.AddRange(ExpectedAttachments);
        result.AddRange(TestDataAttachments);
        if (Attachments != null) result.AddRange(Attachments);
        return result;
    }

    public static Step CopyFrom(Step step)
    {
        return new Step()
        {
            SharedStepId = step.SharedStepId,
            Action = step.Action,
            Expected = step.Expected,
            ActionAttachments = step.ActionAttachments,
            ExpectedAttachments = step.ExpectedAttachments,
            TestDataAttachments = step.TestDataAttachments,
            Attachments = step.Attachments,
            TestData = step.TestData,
        };
    }
}
