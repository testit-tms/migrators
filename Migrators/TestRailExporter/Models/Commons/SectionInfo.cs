using Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace TestRailExporter.Models.Commons;

public class SectionInfo
{
    public Section MainSection { get; set; }
    public Dictionary<int, Guid> SectionsMap { get; set; }
}
