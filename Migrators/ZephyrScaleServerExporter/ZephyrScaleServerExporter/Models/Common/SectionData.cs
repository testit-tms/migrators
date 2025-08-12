using Models;

namespace ZephyrScaleServerExporter.Models.Common;

public class SectionData
{
    public required Section MainSection { get; set; }
    public required Dictionary<string, Guid> SectionMap { get; set; }
    public required Dictionary<string, Section> AllSections { get; set; }
}
