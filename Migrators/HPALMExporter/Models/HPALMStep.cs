namespace ImportHPALMToTestIT.Models.HPALM;

public class HPALMStep
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint ParentId { get; set; }
    public bool HasAttachments { get; set; }
    public string Expected { get; set; }
    public string Description { get; set; }
    public uint Order { get; set; }
    public uint? LinkId { get; set; }
}
