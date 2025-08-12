using Models;

namespace ZephyrScaleServerExporter.Models.TestCases;

public class StepsData
{
    public required List<Step> Steps { get; set; }
    public required List<Iteration> Iterations { get; set; }
}
