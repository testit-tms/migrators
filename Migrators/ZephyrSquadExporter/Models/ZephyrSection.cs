namespace ZephyrSquadExporter.Models;

public class ZephyrSection
{
    public Guid Guid { get; set; }
    public bool IsFolder { get; set; }

    public string CycleId { get; set; }
}
