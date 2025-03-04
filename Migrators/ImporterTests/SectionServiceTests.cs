using Microsoft.Extensions.Logging;
using Importer.Client;
using Importer.Services.Implementations;
using Models;
using NSubstitute;

namespace ImporterTests;

public class SectionServiceTests
{
    private ILogger<SectionService> _logger = null!;
    private IClientAdapter _clientAdapter = null!;
    private SectionService _sectionService = null!;

    private static readonly Guid ProjectId = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private static readonly Guid RootSectionId = Guid.Parse("9f3c5ed5-d7d4-483f-b69f-e68c079cffe8");

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SectionService>>();
        _clientAdapter = Substitute.For<IClientAdapter>();
        _sectionService = new SectionService(_logger, _clientAdapter);

        // Setup get root section
        _clientAdapter.GetRootSectionId(ProjectId).Returns(RootSectionId);
    }

    [Test]
    public async Task ImportSections_WhenNoSections_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _sectionService.ImportSections(ProjectId, Array.Empty<Section>());

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Is.Empty);
            await _clientAdapter.Received(1).GetRootSectionId(ProjectId);
            await _clientAdapter.DidNotReceive().ImportSection(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Section>());
        });
    }

    [Test]
    public async Task ImportSections_WhenSingleSection_ReturnsMapping()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var newSectionId = Guid.NewGuid();
        var section = new Section { Id = sectionId, Name = "Test Section" };

        _clientAdapter.ImportSection(ProjectId, RootSectionId, section).Returns(newSectionId);

        // Act
        var result = await _sectionService.ImportSections(ProjectId, new[] { section });

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[sectionId], Is.EqualTo(newSectionId));
            await _clientAdapter.Received(1).GetRootSectionId(ProjectId);
            await _clientAdapter.Received(1).ImportSection(ProjectId, RootSectionId, section);
        });
    }

    [Test]
    public async Task ImportSections_WhenNestedSections_ReturnsCorrectMapping()
    {
        // Arrange
        var parentSectionId = Guid.NewGuid();
        var childSectionId = Guid.NewGuid();
        var newParentSectionId = Guid.NewGuid();
        var newChildSectionId = Guid.NewGuid();

        var childSection = new Section { Id = childSectionId, Name = "Child Section" };
        var parentSection = new Section
        {
            Id = parentSectionId,
            Name = "Parent Section",
            Sections = new List<Section> { childSection }
        };

        _clientAdapter.ImportSection(ProjectId, RootSectionId, parentSection).Returns(newParentSectionId);
        _clientAdapter.ImportSection(ProjectId, newParentSectionId, childSection).Returns(newChildSectionId);

        // Act
        var result = await _sectionService.ImportSections(ProjectId, new[] { parentSection });

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[parentSectionId], Is.EqualTo(newParentSectionId));
            Assert.That(result[childSectionId], Is.EqualTo(newChildSectionId));

            await _clientAdapter.Received(1).GetRootSectionId(ProjectId);
            await _clientAdapter.Received(1).ImportSection(ProjectId, RootSectionId, parentSection);
            await _clientAdapter.Received(1).ImportSection(ProjectId, newParentSectionId, childSection);
        });
    }

    [Test]
    public async Task ImportSections_WhenMultipleSections_ImportsAllSections()
    {
        // Arrange
        var section1Id = Guid.NewGuid();
        var section2Id = Guid.NewGuid();
        var newSection1Id = Guid.NewGuid();
        var newSection2Id = Guid.NewGuid();

        var section1 = new Section { Id = section1Id, Name = "Section 1" };
        var section2 = new Section { Id = section2Id, Name = "Section 2" };

        _clientAdapter.ImportSection(ProjectId, RootSectionId, section1).Returns(newSection1Id);
        _clientAdapter.ImportSection(ProjectId, RootSectionId, section2).Returns(newSection2Id);

        // Act
        var result = await _sectionService.ImportSections(ProjectId, new[] { section1, section2 });

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[section1Id], Is.EqualTo(newSection1Id));
            Assert.That(result[section2Id], Is.EqualTo(newSection2Id));

            await _clientAdapter.Received(1).GetRootSectionId(ProjectId);
            await _clientAdapter.Received(1).ImportSection(ProjectId, RootSectionId, section1);
            await _clientAdapter.Received(1).ImportSection(ProjectId, RootSectionId, section2);
        });
    }
}
