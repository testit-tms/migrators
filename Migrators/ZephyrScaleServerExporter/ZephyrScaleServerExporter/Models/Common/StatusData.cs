using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Models.Common;

public class StatusData
{
    public required string StringStatuses { get; set; }
    public required Attribute StatusAttribute { get; set; }
}
