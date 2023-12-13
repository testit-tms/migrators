namespace ImportHPALMToTestIT.Models.HPALM;

public class HPALMStep
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ParentId { get; set; }
    public bool HasAttachments { get; set; }
    public string Expected { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public int? LinkId { get; set; }
}
