using System.ComponentModel.DataAnnotations;

namespace TestRailXmlExporter.Models;

public abstract record class CustomAttributeOptionPostModel
{
    /// <summary>
    /// Value of the attribute option
    /// </summary>
    [StringLength(255)]
    public string? Value { get; set; }

    /// <summary>
    /// Indicates if the attribute option is used by default
    /// </summary>
    public bool IsDefault { get; set; }
}
