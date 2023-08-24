using Importer.Client;
using Importer.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ImporterTests;

public class SectionServiceTests
{
    private ILogger<SectionService> _logger;
    private IClient _client;
    private List<Section> _sections;
    private Dictionary<Guid, Guid> _sectionsMap;
    private readonly Guid _rootSectionId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SectionService>>();
        _client = Substitute.For<IClient>();
        _sections = new List<Section>
        {
            new()
            {
                Id = Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c88"),
                Name = "TestSection",
                PreconditionSteps = new List<Step>
                {
                    new()
                    {
                        Action = "TestAction",
                        Expected = "TestExpected"
                    }
                },
                PostconditionSteps = new List<Step>
                {
                    new()
                    {
                        Action = "TestAction",
                        Expected = "TestExpected"
                    }
                },
                Sections = new List<Section>
                {
                    new()
                    {
                        Id = Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781de9"),
                        Name = "TestSection02",
                        PreconditionSteps = new List<Step>
                        {
                            new()
                            {
                                Action = "TestAction",
                                Expected = "TestExpected"
                            }
                        },
                        PostconditionSteps = new List<Step>
                        {
                            new()
                            {
                                Action = "TestAction",
                                Expected = "TestExpected"
                            }
                        },
                        Sections = new List<Section>()
                    }
                }
            }
        };
        _sectionsMap = new Dictionary<Guid, Guid>
        {
            { Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c88"), Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c10") },
            { Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781de9"), Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781d10") }
        };
    }

    [Test]
    public async Task ImportSections_FailedGetRootSection()
    {
        // Arrange
        _client.GetRootSectionId().ThrowsAsync(new Exception("TestException"));

        var sectionService = new SectionService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await sectionService.ImportSections(_sections));

        // Assert
        await _client.DidNotReceive().ImportSection(Arg.Any<Guid>(), Arg.Any<Section>());
    }

    [Test]
    public async Task ImportSections_FailedCreateMainSection()
    {
        // Arrange
        _client.GetRootSectionId().Returns(_rootSectionId);
        _client.ImportSection(_rootSectionId, _sections[0]).ThrowsAsync(new Exception("TestException"));

        var sectionService = new SectionService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await sectionService.ImportSections(_sections));

        // Assert
        await _client.DidNotReceive().ImportSection(Arg.Any<Guid>(), _sections[0].Sections[0]);
    }

    [Test]
    public async Task ImportSections_FailedCreateChildSection()
    {
        // Arrange
        var mainSectionGuid = Guid.NewGuid();
        _client.GetRootSectionId().Returns(_rootSectionId);
        _client.ImportSection(_rootSectionId, _sections[0]).Returns(mainSectionGuid);
        _client.ImportSection(mainSectionGuid, _sections[0].Sections[0]).ThrowsAsync(new Exception("TestException"));

        var sectionService = new SectionService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await sectionService.ImportSections(_sections));
    }

    [Test]
    public async Task ImportSections_Success()
    {
        // Arrange
        var mainSectionGuid = _sectionsMap[_sections[0].Id];
        var childSectionGuid = _sectionsMap[_sections[0].Sections[0].Id];
        _client.GetRootSectionId().Returns(_rootSectionId);
        _client.ImportSection(_rootSectionId, _sections[0]).Returns(mainSectionGuid);
        _client.ImportSection(mainSectionGuid, _sections[0].Sections[0]).Returns(childSectionGuid);

        var sectionService = new SectionService(_logger, _client);

        // Act
        var resp = await sectionService.ImportSections(_sections);

        // Assert
        Assert.Equals(resp, _sectionsMap);
    }
}
