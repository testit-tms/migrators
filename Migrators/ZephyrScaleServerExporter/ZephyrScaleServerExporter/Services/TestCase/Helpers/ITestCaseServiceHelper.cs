using Models;

namespace ZephyrScaleServerExporter.Services.TestCase.Helpers;

public interface ITestCaseServiceHelper
{
    List<Iteration> SanitizeIterations(List<Iteration>? iterations);

    List<T> ExcludeDuplicates<T>(List<T> list);

    PriorityType ConvertPriority(string? priority);

    StateType ConvertStatus(string? status);

    void ExcludeLongTags(global::Models.TestCase testcase);
}