using AllureExporter.Client;
using AllureExporter.Models;
using AllureExporter.Services;
using AllureExporter.Services.Implementations;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AllureExporterTests;

public class SectionServiceTests
{
    private ILogger<SectionService> _logger;
    private IClient _client;
    private const int ProjectId = 1;
    private List<BaseEntity> _suites;
    private SectionInfo _sectionInfo;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SectionService>>();
        _client = Substitute.For<IClient>();
        _suites = new List<BaseEntity>
        {
            new()
            {
                Id = 1,
                Name = "TestSuite1"
            },
            new()
            {
                Id = 2,
                Name = "TestSuite2"
            }
        };

        _sectionInfo = new SectionInfo
        {
            MainSection = new Section
            {
                Id = Guid.NewGuid(),
                Name = "Allure",
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
            },
            SectionDictionary = new Dictionary<long, Guid>
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
        _client.GetSuites(ProjectId)
            .ThrowsAsync(new Exception("Failed to get suites"));

        var service = new SectionService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ConvertSection(ProjectId));
    }

    [Test]
    public async Task ConvertSection_Success()
    {
        // Arrange
        _client.GetSuites(ProjectId).Returns(_suites);

        var service = new SectionService(_logger, _client);

        // Act
        var result = await service.ConvertSection(ProjectId);

        // Assert
        Assert.That(result.SectionDictionary, Has.Count.EqualTo(_sectionInfo.SectionDictionary.Count));
        Assert.That(result.MainSection.Name, Is.EqualTo(_sectionInfo.MainSection.Name));
        Assert.That(result.MainSection.Sections, Has.Count.EqualTo(_sectionInfo.MainSection.Sections.Count));
        Assert.That(result.MainSection.Sections[0].Name, Is.EqualTo(_sectionInfo.MainSection.Sections[0].Name));
        Assert.That(result.MainSection.Sections[1].Name, Is.EqualTo(_sectionInfo.MainSection.Sections[1].Name));
    }
}
