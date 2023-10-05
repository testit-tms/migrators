namespace ZephyrScaleExporter.Models;

public class ZephyrDescriptionData
{
    public string Description { get; set; }
    public List<ZephyrAttachment> Attachments { get; set; }
}

public class ZephyrAttachment
{
    public string FileName { get; set; }
    public string Url { get; set; }
}
