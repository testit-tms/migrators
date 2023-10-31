using Models;

namespace XRayExporter.Models;

public class SectionData
{
    public List<Section> Sections { get; set; }
    public Dictionary<int, Guid> SectionMap { get; set; }
}
