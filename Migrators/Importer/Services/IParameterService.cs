using Importer.Models;
using Models;

namespace Importer.Services;

public interface IParameterService
{
    Task<List<TmsParameter>> CreateParameters(IEnumerable<Parameter> parameters);
}