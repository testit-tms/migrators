using Models;

namespace QaseExporter.Models;

public class StepsData
{
    public List<Step> Steps { get; set; } = new();
    public List<Iteration> Iterations { get; set; } = new();
}
