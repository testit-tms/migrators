using QaseExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using QaseExporter.Models;

namespace QaseExporter.Services;

public class SectionService : ISectionService
{
    private readonly ILogger<SectionService> _logger;
    private readonly IClient _client;
    private readonly Dictionary<int, Guid> _sectionMap;
        private const string MainSectionName = "Qase";

    public SectionService(ILogger<SectionService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
        _sectionMap = new Dictionary<int, Guid>();
    }

    public async Task<SectionData> ConvertSections()
    {
        _logger.LogInformation("Converting test suites");

        var allSuites = await _client.GetSuites();

        _logger.LogDebug("Found {Count} test suites", allSuites.Count);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = MainSectionName,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = ConvertSuitesToSections(allSuites)
        };

        var sectionData = new SectionData
        {
            MainSection = section,
            SectionMap = _sectionMap
        };

        _logger.LogInformation("Exported test suites");

        return sectionData;
    }

    private List<Section> ConvertSuitesToSections(List<QaseSuite> suites)
    {
        var sections = new List<Section>();

        foreach (var suite in suites)
        {
            if (_sectionMap.ContainsKey(suite.Id))
            {
                continue;
            }

            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = suite.Name,
                PreconditionSteps = new List<Step> {
                        new() {
                            Action = suite.Preconditions
                        }
                    },
                PostconditionSteps = new List<Step>(),
                Sections = ConvertSuitesToSections(suites.Where(s => s.ParentId.Equals(suite.Id)).ToList()),
            };

            _sectionMap.Add(suite.Id, section.Id);

            sections.Add(section);
        }

        return sections;
    }
}
