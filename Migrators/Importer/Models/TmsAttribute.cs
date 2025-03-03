namespace Importer.Models;

public class TmsAttribute
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsRequired { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
    public List<TmsAttributeOptions> Options { get; set; } = new();
}