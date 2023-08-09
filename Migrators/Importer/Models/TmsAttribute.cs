namespace Importer.Models;

public class TmsAttribute
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsRequired { get; set; }
    public string Type { get; set; }
    public bool IsGlobal { get; set; }
    public IEnumerable<TmsAttributeOptions> Options { get; set; }
}
