using Models;

namespace ZephyrSquadExporter.Models;

public class SectionData
{
    public List<Section> Sections { get; set; }
    public Dictionary<string, ZephyrSection> SectionMap { get; set; }
}
