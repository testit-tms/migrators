using AllureExporter.Client;
using AllureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;
using Constants = AllureExporter.Models.Constants;

namespace AllureExporter.Services;

public class SectionService : ISectionService
{
    private readonly ILogger<SectionService> _logger;
    private readonly IClient _client;

    private const string MainSectionName = "Allure";

    public SectionService(ILogger<SectionService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<SectionInfo> ConvertSection(long projectId)
    {
        _logger.LogInformation("Converting sections");

        var sectionIdMap = new Dictionary<long, Guid>();
        var sections = await _client.GetSuites(projectId);

        _logger.LogDebug("Found {Count} sections: {@Sections}", sections.Count, sections);

        var childSections = new List<Section>();

        foreach (var s in sections)
        {
            var childSection = new Section
            {
                Id = Guid.NewGuid(),
                Name = s.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = new List<Section>()
            };
            childSections.Add(childSection);
            sectionIdMap.Add(s.Id, childSection.Id);
        }

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = MainSectionName,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = childSections
        };
        sectionIdMap.Add(Constants.MainSectionId, section.Id);

        _logger.LogDebug("Converted sections: {@Section}", section);

        _logger.LogInformation("Ending converting sections");

        return new SectionInfo
        {
            MainSection = section,
            SectionDictionary = sectionIdMap
        };
    }
}
