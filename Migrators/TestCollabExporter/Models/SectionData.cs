using Models;

namespace TestCollabExporter.Models;

public class SectionData
{
    public List<Section> Sections { get; set; }
    public Dictionary<int, Guid> SectionMap { get; set; }
    public Section SharedStepSection { get; set; }
}
