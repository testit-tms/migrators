using Models;

namespace TestLinkExporter.Models.Suite;

public class SectionData
{
    public List<Section> Sections { get; set; }
    public Dictionary<int, Guid> SectionMap { get; set; }
}
