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
    private List<QaseSuite> _allSuites;
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

        _allSuites = await _client.GetSuites();

        _logger.LogDebug("Found {Count} test suites", _allSuites.Count);

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = MainSectionName,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = ConvertSuitesToSections(_allSuites)
        };

        var sectionData = new SectionData
        {
            MainSection = section,
            SectionMap = _sectionMap
        };

        _logger.LogInformation("Exported test suites");

        return sectionData;
    }

    private List<Section> ConvertSuitesToSections(List<QaseSuite> childSuites)
    {
        var sections = new List<Section>();

        foreach (var childSuite in childSuites)
        {
            if (_sectionMap.ContainsKey(childSuite.Id))
            {
                continue;
            }

            var section = new Section
            {
                Id = Guid.NewGuid(),
                Name = childSuite.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = ConvertSuitesToSections(_allSuites.Where(s => s.ParentId.Equals(childSuite.Id)).ToList()),
            };

            if (childSuite.Description != "")
            {
                section.PreconditionSteps.Add(
                    new() {
                        Action = childSuite.Description
                    }
                );
            }

            if (childSuite.Preconditions != "")
            {
                section.PreconditionSteps.Add(
                    new()
                    {
                        Action = childSuite.Preconditions
                    }
                );
            }

            _sectionMap.Add(childSuite.Id, section.Id);

            sections.Add(section);
        }

        return sections;
    }
}
