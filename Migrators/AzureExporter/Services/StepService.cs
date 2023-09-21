using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Models;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Common;

namespace AzureExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;

    public StepService(ILogger<StepService> logger)
    {
        _logger = logger;
    }

    public async Task<List<Step>> ConvertSteps(string stepsContent, Dictionary<int, Guid> sharedStepMap)
    {
        _logger.LogDebug("Found steps: {@AzureSteps}", stepsContent);

        var azureSteps = ReadTestCaseStepsFromXmlContent(stepsContent);

        var steps = azureSteps.Steps.Select(azureStep => ConvertStep(azureStep)).ToList();

        foreach (var sharedStep in azureSteps.SharedSteps)
        {
            if (!sharedStepMap.IsNullOrEmpty())
            {
                steps.Add(
                    new Step
                    {
                        SharedStepId = sharedStepMap[int.Parse(sharedStep.Id)]
                    }
                );
            }

            steps.AddRange(sharedStep.Steps.Select(azureStep => ConvertStep(azureStep)).ToList());
        }

        return steps;
    }

    private Step ConvertStep(AzureStep azureStep)
    {
        return new Step
        {
            Action = azureStep.Values[0],
            Expected = azureStep.Values[1]
        };
    }

    private AzureSteps ReadTestCaseStepsFromXmlContent(string stepsContent)
    {
        var azureSteps = new AzureSteps();

        using (TextReader stepsReader = new StringReader(stepsContent))
        {
            var serializer = new XmlSerializer(typeof(AzureSteps));
            azureSteps = (AzureSteps)serializer.Deserialize(stepsReader);
        }

        return azureSteps;
    }

    // private string StripHTML(string text)
    // {
    //     return Regex.Replace(text, "<.*?>", String.Empty);
    // }
}

