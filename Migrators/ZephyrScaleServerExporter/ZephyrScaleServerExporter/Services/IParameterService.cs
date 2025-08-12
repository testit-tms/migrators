using Models;

namespace ZephyrScaleServerExporter.Services;

public interface IParameterService
{
    Task<List<Iteration>> ConvertParameters(string testCaseKey);
    List<Iteration> MergeIterations(List<Iteration> mainIterations, List<Iteration> subIterations);
}
