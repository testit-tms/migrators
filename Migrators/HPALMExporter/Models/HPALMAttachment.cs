namespace HPALMExporter.Models;

public class HPALMAttachment
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint ParentId { get; set; }
    public string Description { get; set; }
    public HPALMAttachmentType Type { get; set; }
}
