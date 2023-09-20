using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Models;

namespace AzureExporter.Services;

public interface IStepService
{
    abstract List<Step> ConvertSteps(string steps);

    void ReadTestCaseSteps(WorkItem testCase);
}
