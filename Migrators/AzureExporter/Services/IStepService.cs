using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Models;

namespace AzureExporter.Services;

public interface IStepService
{
    void ReadTestCaseSteps(WorkItem testCase);

    Task<List<Step>> ConvertSteps(string? steps);
}
