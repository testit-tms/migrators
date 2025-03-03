using Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace AllureExporter.Models.Project;

public class SectionInfo
{
    public Section MainSection { get; set; }
    public Dictionary<long, Guid> SectionDictionary { get; set; }
}
