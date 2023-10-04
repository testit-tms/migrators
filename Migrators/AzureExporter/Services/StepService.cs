using System.Xml.Serialization;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace AzureExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;

    public StepService(ILogger<StepService> logger)
    {
        _logger = logger;
    }

    public List<Step> ConvertSteps(string stepsContent, Dictionary<int, Guid> sharedStepMap)
    {
        _logger.LogDebug("Found steps: {@AzureSteps}", stepsContent);

        if (string.IsNullOrWhiteSpace(stepsContent))
        {
            return new List<Step>();
        }

        var azureSteps = ParseStepsFromXmlContent(stepsContent);

        var steps = azureSteps.Steps.Select(ConvertStep).ToList();

        foreach (var sharedStep in azureSteps.SharedSteps)
        {
            if (sharedStepMap.TryGetValue(sharedStep.Id, out var sharedStepId))
            {
                steps.Add(new Step
                    {
                        SharedStepId = sharedStepId,
                        ActionAttachments = new List<string>(),
                        Action = string.Empty,
                        Expected = string.Empty
                    }
                );
            }
            else
            {
                throw new ApplicationException("Shared step not found in map");
            }

            steps.AddRange(sharedStep.Steps.Select(ConvertStep).ToList());
        }

        _logger.LogDebug("Converted steps: {@Steps}", steps);

        return steps;
    }

    private static Step ConvertStep(AzureStep azureStep)
    {
        return new Step
        {
            Action = azureStep.Values[0],
            Expected = azureStep.Values[1],
            ActionAttachments = new List<string>()
        };
    }

    private static AzureSteps ParseStepsFromXmlContent(string stepsContent)
    {
        using TextReader stepsReader = new StringReader(stepsContent);
        var serializer = new XmlSerializer(typeof(AzureSteps));
        var azureSteps = (AzureSteps)serializer.Deserialize(stepsReader)!;

        return azureSteps;
    }
}
