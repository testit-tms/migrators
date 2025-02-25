namespace TestRailExporter.Models;

public class TestRailDescriptionInfo
{
    public string Description { get; set; } = string.Empty;
    public List<string> AttachmentNames { get; set; } = new();
}
