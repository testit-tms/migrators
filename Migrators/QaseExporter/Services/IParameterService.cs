using Models;

namespace QaseExporter.Services;

public interface IParameterService
{
    List<Iteration> ConvertParameters(Dictionary<string, List<string>> testCaseKey);
}
