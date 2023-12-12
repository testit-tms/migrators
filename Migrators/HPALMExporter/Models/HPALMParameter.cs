namespace ImportHPALMToTestIT.Models.HPALM;

public class HPALMParameter
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint ParentId { get; set; }
    public bool IsAssign { get; set; }
    public string Description { get; set; }
    public string Value { get; set; }
}
