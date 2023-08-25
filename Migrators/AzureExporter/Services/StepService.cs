using AzureExporter.Client;
using AzureExporter.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using Models;
using System.Xml;
using Newtonsoft.Json;

namespace AzureExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;

    public StepService(ILogger<StepService> logger)
    {
        _logger = logger;
    }

    public List<Step> ConvertSteps(string steps)
    {
        var xmldoc = new XmlDocument();
        xmldoc.LoadXml(steps);
        var fromXml = JsonConvert.SerializeXmlNode(xmldoc);
        var azureSteps = JsonConvert.DeserializeObject<AzureSteps>(fromXml);

        _logger.LogDebug("Found steps: {@AzureSteps}", azureSteps);

        return azureSteps.Steps.Select(azureStep =>
        {
            var step = new Step
            {
                Action = azureStep.Values[0],
                Expected = azureStep.Values[1]
            };
            return step;
        })
            .ToList();
    }
}
