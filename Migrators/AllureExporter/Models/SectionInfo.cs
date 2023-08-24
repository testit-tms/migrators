using Models;

namespace AllureExporter.Models;

public class SectionInfo
{
    public Section MainSection { get; set; }
    public Dictionary<int, Guid> SectionDictionary { get; set; }
}
