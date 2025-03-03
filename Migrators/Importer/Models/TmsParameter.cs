namespace Importer.Models;

public class TmsParameter
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid ParameterKeyId { get; set; }
}