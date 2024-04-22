namespace ZephyrSquadServerExporter.Models;

public class ZephyrSection
{
    public Guid Id { get; set; }
    public string ProjectId { get; set; }
    public string VersionId { get; set; }
    public string CycleId { get; set; }
    public string FolderId { get; set; }
}
