using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using TestRailImporter.Enums;

namespace TestRailImporter.Models;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public record CustomAttributeModel : CustomAttributeBaseModel
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
