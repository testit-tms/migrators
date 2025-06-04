using TestLinkExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TestLinkExporter.Services.Implementations;
using TestLinkExporter.Models.Suite;

namespace TestLinkExporterTests;

public class SectionServiceTests
{
    private ILogger<SectionService> _logger;
    private IClient _client;
    private const int ProjectId = 1;
    private List<TestLinkSuite> _suites;
    private SectionData _sectionData;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SectionService>>();
        _client = Substitute.For<IClient>();
        _suites = new List<TestLinkSuite>
        {
            new()
            {
                Id = 1,
                Name = "MainSuite",
                Suites = new List<TestLinkSuite>() {
                    new()
                    {
                        Id = 2,
                        Name = "TestSuite1",
                        Suites = new List<TestLinkSuite>()
                    },
                    new()
                    {
                        Id = 3,
                        Name = "TestSuite2",
                        Suites = new List<TestLinkSuite>()
                    }
                }
            },
        };

        _sectionData = new SectionData
        {
            Sections = new List<Section> {
                new Section
                {
                    Id = Guid.NewGuid(),
                    Name = "MainSuite",
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Sections = new List<Section>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "TestSuite1",
                            PreconditionSteps = new List<Step>(),
                            PostconditionSteps = new List<Step>(),
                            Sections = new List<Section>()
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Name = "TestSuite2",
                            PreconditionSteps = new List<Step>(),
                            PostconditionSteps = new List<Step>(),
                            Sections = new List<Section>()
                        }
                    }
                }
            },
            SectionMap = new Dictionary<int, Guid>
            {
                { 0, Guid.NewGuid() },
                { 1, Guid.NewGuid() },
                { 2, Guid.NewGuid() }
            }
        };
    }

    [Test]
    public async Task ConvertSection_FailedGetSuites()
    {
        // Arrange
        _client.GetSuitesByProjectId(ProjectId)
            .Throws(new Exception("Failed to get suites"));

        var service = new SectionService(_logger, _client);

        // Act
        Assert.Throws<Exception>(() => service.ConvertSections(ProjectId));
    }

    [Test]
    public async Task ConvertSection_FailedGetSharedSuites()
    {
        // Arrange
        _client.GetSuitesByProjectId(ProjectId).Returns(_suites);
        _client.GetSharedSuitesBySuiteId(_suites[0].Id)
            .Throws(new Exception("Failed to get shared suites"));

        var service = new SectionService(_logger, _client);

        // Act
        Assert.Throws<Exception>(() => service.ConvertSections(ProjectId));
    }

    [Test]
    public async Task ConvertSection_Success()
    {
        // Arrange
        _client.GetSuitesByProjectId(ProjectId).Returns(_suites);
        _client.GetSharedSuitesBySuiteId(_suites[0].Id).Returns(_suites[0].Suites);
        _client.GetSharedSuitesBySuiteId(_suites[0].Suites[0].Id).Returns(_suites[0].Suites[0].Suites);
        _client.GetSharedSuitesBySuiteId(_suites[0].Suites[1].Id).Returns(_suites[0].Suites[1].Suites);
        var service = new SectionService(_logger, _client);

        // Act
        var result = service.ConvertSections(ProjectId);

        // Assert
        Assert.That(result.SectionMap, Has.Count.EqualTo(_sectionData.SectionMap.Count));
        Assert.That(result.Sections[0].Name, Is.EqualTo(_sectionData.Sections[0].Name));
        Assert.That(result.Sections[0].Sections, Has.Count.EqualTo(_sectionData.Sections[0].Sections.Count));
        Assert.That(result.Sections[0].Sections[0].Name, Is.EqualTo(_sectionData.Sections[0].Sections[0].Name));
        Assert.That(result.Sections[0].Sections[1].Name, Is.EqualTo(_sectionData.Sections[0].Sections[1].Name));
    }
}
