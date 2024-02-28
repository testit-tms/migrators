using System.ComponentModel.DataAnnotations;

namespace TestRailImporter.Models;

public abstract record class CustomAttributeBaseModel
{
    /// <summary>
    /// Name of the attribute
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Indicates if the attribute is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Indicates if the attribute value is mandatory to specify
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Indicates if the attribute is available across all projects
    /// </summary>
    public bool IsGlobal { get; set; }
}
