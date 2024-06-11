using Models;

namespace ZephyrScaleServerExporter.Models;

public class SectionData
{
    public Section MainSection { get; set; }
    public Dictionary<string, Guid> SectionMap { get; set; }
    public Dictionary<string, Section> AllSections { get; set; }
}
