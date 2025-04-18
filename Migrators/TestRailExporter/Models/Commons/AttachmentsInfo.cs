using Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace TestRailExporter.Models.Commons;

public class AttachmentsInfo
{
    public List<string> AttachmentNames { get; set; }
    public Dictionary<string, string> AttachmentsMap { get; set; }
}
