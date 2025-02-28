using Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace TestRailExporter.Models.Commons;

public class StepsInfo
{
    public List<Step> Steps { get; set; }
    public List<string> StepAttachmentNames { get; set; }
}
