using Models;

namespace QaseExporter.Models;

public class SectionData
{
    public Section MainSection { get; set; }
    public Dictionary<int, Guid> SectionMap { get; set; }
}
