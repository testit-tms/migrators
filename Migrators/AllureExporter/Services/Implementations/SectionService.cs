using AllureExporter.Client;
using AllureExporter.Models.Project;
using Microsoft.Extensions.Logging;
using Models;
using Constants = AllureExporter.Models.Project.Constants;

namespace AllureExporter.Services.Implementations;

internal class SectionService(ILogger<SectionService> logger, IClient client) : ISectionService
{
    private const string MainSectionName = "Allure";

    public async Task<SectionInfo> ConvertSection(long projectId)
    {
        logger.LogInformation("Converting sections");

        var sectionIdMap = new Dictionary<long, Guid>();
        var sections = await client.GetSuites(projectId);

        logger.LogDebug("Found {Count} sections: {@Sections}", sections.Count, sections);

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

        logger.LogDebug("Converted sections: {@Section}", section);

        logger.LogInformation("Ending converting sections");

        return new SectionInfo
        {
            MainSection = section,
            SectionDictionary = sectionIdMap
        };
    }
}
