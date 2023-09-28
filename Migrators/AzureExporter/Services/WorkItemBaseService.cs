using Models;

namespace AzureExporter.Services;

public class WorkItemBaseService
{
    protected static List<string> ConvertTags(string tagsContent)
    {
        return string.IsNullOrEmpty(tagsContent)
            ? new List<string>()
            : tagsContent.Split("; ").ToList();
    }

    protected static PriorityType ConvertPriority(int priority)
    {
        return priority switch
        {
            1 => PriorityType.Highest,
            2 => PriorityType.High,
            3 => PriorityType.Medium,
            4 => PriorityType.Low,
            _ => throw new Exception($"Failed to convert priority {priority}")
        };
    }
}
