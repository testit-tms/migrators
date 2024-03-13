using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using TestRailExporter.Enums;

namespace TestRailExporter.Models;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public record class CustomAttributeModel : CustomAttributeBaseModel
{
    /// <summary>
    /// Unique ID of the attribute
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Collection of the attribute options
    /// <br/>
    /// Available for attributes of type `options` and `multiple options` only
    /// </summary>
    public List<CustomAttributeOptionModel>? Options { get; set; } = new();

    /// <summary>
    /// Type of the attribute
    /// </summary>
    [Required]
    public CustomAttributeTypesEnum Type { get; set; }

    /// <summary>
    /// Indicates if the attribute is deleted
    /// </summary>
    public bool IsDeleted { get; set; }
}
