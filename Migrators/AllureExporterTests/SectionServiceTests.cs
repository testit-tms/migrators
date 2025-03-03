using AllureExporter.Client;
using AllureExporter.Models.Project;
using AllureExporter.Services.Implementations;
using Microsoft.Extensions.Logging;
using Models;
using Moq;

namespace AllureExporterTests;

public class SectionServiceTests
{
    private Mock<ILogger<SectionService>> _logger;
    private Mock<IClient> _client;
    private SectionService _sut;
    private const int ProjectId = 1;
    private List<BaseEntity> _suites;
    private SectionInfo _expectedSectionInfo;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<SectionService>>();
        _client = new Mock<IClient>();
        _sut = new SectionService(_logger.Object, _client.Object);

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

        _expectedSectionInfo = new SectionInfo
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
    public void ConvertSection_FailedGetSuites()
    {
        // Arrange
        var expectedErrorMessage = "Failed to get suites";
        _client.Setup(x => x.GetSuites(ProjectId))
            .ThrowsAsync(new Exception(expectedErrorMessage));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _sut.ConvertSection(ProjectId));
        Assert.That(ex.Message, Is.EqualTo(expectedErrorMessage));
    }

    [Test]
    public async Task ConvertSection_Success()
    {
        // Arrange
        _client.Setup(x => x.GetSuites(ProjectId))
            .ReturnsAsync(_suites);

        // Act
        var result = await _sut.ConvertSection(ProjectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.SectionDictionary, Has.Count.EqualTo(_expectedSectionInfo.SectionDictionary.Count));
            Assert.That(result.MainSection.Name, Is.EqualTo(_expectedSectionInfo.MainSection.Name));
            Assert.That(result.MainSection.Sections, Has.Count.EqualTo(_expectedSectionInfo.MainSection.Sections.Count));

            for (var i = 0; i < result.MainSection.Sections.Count; i++)
            {
                Assert.That(result.MainSection.Sections[i].Name, Is.EqualTo(_expectedSectionInfo.MainSection.Sections[i].Name));
                Assert.That(result.MainSection.Sections[i].PreconditionSteps, Is.Empty);
                Assert.That(result.MainSection.Sections[i].PostconditionSteps, Is.Empty);
                Assert.That(result.MainSection.Sections[i].Sections, Is.Empty);
            }
        });

        _client.Verify(x => x.GetSuites(ProjectId), Times.Once);
    }

    [Test]
    public async Task ConvertSection_EmptySuites_ReturnsEmptySection()
    {
        // Arrange
        _client.Setup(x => x.GetSuites(ProjectId))
            .ReturnsAsync(new List<BaseEntity>());

        // Act
        var result = await _sut.ConvertSection(ProjectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.MainSection.Name, Is.EqualTo("Allure"));
            Assert.That(result.MainSection.Sections, Is.Empty);
            Assert.That(result.MainSection.PreconditionSteps, Is.Empty);
            Assert.That(result.MainSection.PostconditionSteps, Is.Empty);
            Assert.That(result.SectionDictionary, Has.Count.EqualTo(1));
        });
    }
}
