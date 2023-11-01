using TestLinkExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using TestLinkExporter.Models;

namespace TestLinkExporter.Services;

public class SectionService : ISectionService
{
    private readonly ILogger<SectionService> _logger;
    private readonly IClient _client;
    private readonly Dictionary<int, Guid> _sectionMap;

    public SectionService(ILogger<SectionService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
        _sectionMap = new Dictionary<int, Guid>();
    }

    public SectionData ConvertSections(int projectId)
    {
        _logger.LogInformation("Converting test suites");

        var mainSuites = _client.GetSuitesByProjectId(projectId);

        _logger.LogDebug("Found {Count} test suites", mainSuites.Count);

        var sections = ConvertSuitesToSections(mainSuites);

        var sectionData = new SectionData
        {
            Sections = sections,
            SectionMap = _sectionMap
        };

        _logger.LogInformation("Exported test suites");

        return sectionData;
    }

    private List<Section> ConvertSharedSections(int mainSuiteId)
    {
        _logger.LogInformation("Converting shared test suites");

        var sharedSuites = _client.GetSharedSuitesBySuiteId(mainSuiteId);

        _logger.LogDebug("Found {Count} test suites", sharedSuites.Count);

        var sections = ConvertSuitesToSections(sharedSuites);

        _logger.LogInformation("Exported shared test suites");

        return sections;
    }

    private List<Section> ConvertSuitesToSections(List<TestLinkSuite> testLinkSuites)
    {
        var sections = new List<Section>();

        foreach (var testLinkSuite in testLinkSuites)
        {
            var sectionId = Guid.NewGuid();

            sections.Add(
                new Section
                {
                    Id = sectionId,
                    Name = testLinkSuite.Name,
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Sections = ConvertSharedSections(testLinkSuite.Id),
                }
            );

            _sectionMap.Add(testLinkSuite.Id, sectionId);
        }

        return sections;
    }
}
