namespace ImportHPALMToTestIT.Models.HPALM;

public class HPALMTest
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public bool IsTemplate { get; set; }
    public bool HasAttachments { get; set; }
    public int ParentId { get; set; }
    public string Status { get; set; }
    public string Author { get; set; }
    public Dictionary<string, string> Attrubites { get; set; }
    public string CreationTime { get; set; }
}
