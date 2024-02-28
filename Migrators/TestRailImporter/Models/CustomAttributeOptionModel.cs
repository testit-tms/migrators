using Newtonsoft.Json;

namespace TestRailImporter.Models;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public record class CustomAttributeOptionModel : CustomAttributeOptionPostModel
{
    /// <summary>
    /// Unique ID of the attribute option
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Indicates if the attributes option is deleted
    /// </summary>
    public bool IsDeleted { get; set; }
}
