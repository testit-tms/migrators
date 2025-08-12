using Microsoft.Extensions.Logging;
using Models;

namespace ZephyrScaleServerExporter.Services.TestCase.Helpers.Implementations;

public class TestCaseServiceHelper(
    IDetailedLogService detailedLogService,
    ILogger<TestCaseServiceHelper> logger) 
    : ITestCaseServiceHelper
{
    /// <summary>
    /// make "iterations": [{"parameters": []}] -> "iterations": [] 
    /// </summary>
    public List<Iteration> SanitizeIterations(List<Iteration>? iterations)
    {
        if (iterations == null)
            return [];
        detailedLogService.LogDebug("Sanitizing iterations: {List}", iterations);
        return iterations.Where(x => x.Parameters.Count > 0).ToList();
    }
    
    public List<T> ExcludeDuplicates<T>(List<T> list)
    {
        var newList = list.ToHashSet().ToList();
        if (newList.Count != list.Count)
        {
            detailedLogService.LogInformation("Excluding attachment link duplicates: {List}", list);
        }
        return newList;
    }
    
    public PriorityType ConvertPriority(string? priority)
    {
        return priority switch
        {
            "High" => PriorityType.High,
            "Normal" => PriorityType.Medium,
            "Low" => PriorityType.Low,
            _ => PriorityType.Medium
        };
    }
    
    public StateType ConvertStatus(string? status)
    {
        return status switch
        {
            "Approved" => StateType.Ready,
            "Draft" => StateType.NotReady,
            "Deprecated" => StateType.NeedsWork,
            _ => StateType.NotReady
        };
    }
    
    
    public void ExcludeLongTags(global::Models.TestCase testcase)
    {
        testcase.Tags = testcase.Tags.Where(x => {
            if (x.Length > 30)
            {
                logger.LogWarning("Tag {X} in {TestCaseName} is longer than 30 symbols, skipping...", 
                    x, testcase.Name);
                return false;
            }
            return true;
        }).ToList();
    }
}