using AzureExporter.Models;

namespace AzureExporter.Services;

public interface IParameterService
{
    List<Dictionary<string, string>> ConvertParameters(AzureParameters parameters);
}
