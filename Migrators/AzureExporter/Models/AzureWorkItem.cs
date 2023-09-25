namespace AzureExporter.Models;

public class AzureWorkItem
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string State { get; set; }

    public int Priority { get; set; }

    public string Steps { get; set; }

    public string IterationPath { get; set; }

    public string Tags { get; set; }

    public List<AzureLink> Links { get; set; }

    public List<AzureAttachment> Attachments { get; set; }
}
