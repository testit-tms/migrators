using Microsoft.Extensions.Logging;
using Models;
using TestCollabExporter.Client;
using TestCollabExporter.Models;

namespace TestCollabExporter.Services;

public class SectionService
{
    private readonly ILogger<SectionService> _logger;
    private readonly IClient _client;

    public SectionService(ILogger<SectionService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<SectionData> ConvertSection(int projectId)
    {
        _logger.LogInformation("Getting sections");

        var suites = await _client.GetSuites(projectId);

        var sharedStepsSection = GetSharedStepsSection();
        var sections = new List<Section>();
        var sectionMap = new Dictionary<int, Guid>(suites.Count);

        foreach (var suite in suites.Where(s => s.Parent_id == 0))
        {

        }
    }

    private Section GetSharedStepsSection()
    {
        return new Section
        {
            Id = Guid.NewGuid(),
            Name = "Shared Steps",
            PostconditionSteps = new List<Step>(),
            PreconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        };
    }
}
