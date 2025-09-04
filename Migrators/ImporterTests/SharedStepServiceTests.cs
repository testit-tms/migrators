using Microsoft.Extensions.Logging;
using Importer.Client;
using Importer.Services.Implementations;
using Importer.Models;
using Importer.Services;
using Models;
using NSubstitute;

namespace ImporterTests;

public class SharedStepServiceTests
{
    private ILogger<SharedStepService> _logger = null!;
    private IClientAdapter _clientAdapter = null!;
    private IParserService _parserService = null!;
    private IAttachmentService _attachmentService = null!;
    private IBaseWorkItemService _baseWorkItemService = null!;
    private SharedStepService _sharedStepService = null!;

    private static readonly Guid ProjectId = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private static readonly Guid SectionId = Guid.Parse("9f3c5ed5-d7d4-483f-b69f-e68c079cffe8");
    private static readonly Guid NewSectionId = Guid.Parse("7d1a4ec6-e8c5-494f-c70f-f79d168dffe9");

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SharedStepService>>();
        _clientAdapter = Substitute.For<IClientAdapter>();
        _parserService = Substitute.For<IParserService>();
        _baseWorkItemService = Substitute.For<IBaseWorkItemService>();
        _attachmentService = Substitute.For<IAttachmentService>();
        _sharedStepService = new SharedStepService(_logger, _clientAdapter,
            _parserService, _baseWorkItemService, _attachmentService);
    }

    [Test]
    public async Task ImportSharedSteps_WhenNoSharedSteps_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _sharedStepService.ImportSharedSteps(
            ProjectId,
            Array.Empty<Guid>(),
            new Dictionary<Guid, Guid>(),
            new Dictionary<Guid, TmsAttribute>()
        );

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Is.Empty);
            await _parserService.DidNotReceive().GetSharedStep(Arg.Any<Guid>());
            await _clientAdapter.DidNotReceive().ImportSharedStep(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<SharedStep>());
        });
    }

    [Test]
    public async Task ImportSharedSteps_WhenSharedStepWithoutAttributesAndAttachments_ImportsSuccessfully()
    {
        // Arrange
        var sharedStepId = Guid.NewGuid();
        var newSharedStepId = Guid.NewGuid();

        var sharedStep = new SharedStep
        {
            Id = sharedStepId,
            Name = "Test Shared Step",
            SectionId = SectionId,
            Steps = new List<Step> { new() { Action = "Test Action" } }
        };

        var sectionsMap = new Dictionary<Guid, Guid> { { SectionId, NewSectionId } };

        _parserService.GetSharedStep(sharedStepId).Returns(sharedStep);
        _attachmentService.GetAttachments(sharedStepId, Arg.Any<IEnumerable<string>>())
            .Returns(new Dictionary<string, Guid>());
        _clientAdapter.ImportSharedStep(ProjectId, NewSectionId, Arg.Any<SharedStep>())
            .Returns(newSharedStepId);

        // Act
        var result = await _sharedStepService.ImportSharedSteps(
            ProjectId,
            new[] { sharedStepId },
            sectionsMap,
            new Dictionary<Guid, TmsAttribute>()
        );

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[sharedStepId], Is.EqualTo(newSharedStepId));

            await _parserService.Received(1).GetSharedStep(sharedStepId);
            await _attachmentService.Received(1).GetAttachments(sharedStepId, Arg.Any<IEnumerable<string>>());
            await _clientAdapter.Received(1).ImportSharedStep(ProjectId, NewSectionId, Arg.Any<SharedStep>());
        });
    }

    [Test]
    public async Task ImportSharedSteps_WhenSharedStepWithAttributesAndAttachments_ImportsSuccessfully()
    {
        // Arrange
        var sharedStepId = Guid.NewGuid();
        var newSharedStepId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();

        var sharedStep = new SharedStep
        {
            Id = sharedStepId,
            Name = "Test Shared Step",
            SectionId = SectionId,
            Steps = new List<Step> { new() { Action = "Test Action" } },
            Attributes = new List<CaseAttribute>
            {
                new() { Id = attributeId, Value = "Test Value" }
            },
            Attachments = new List<string> { "test.txt" }
        };

        var sectionsMap = new Dictionary<Guid, Guid> { { SectionId, NewSectionId } };
        var attributesMap = new Dictionary<Guid, TmsAttribute>
        {
            { attributeId, new TmsAttribute { Id = Guid.NewGuid(), Name = "Test Attribute" } }
        };
        var attachmentsMap = new Dictionary<string, Guid> { { "test.txt", attachmentId } };

        _parserService.GetSharedStep(sharedStepId).Returns(sharedStep);
        _attachmentService.GetAttachments(sharedStepId, Arg.Any<IEnumerable<string>>())
            .Returns(attachmentsMap);
        _clientAdapter.ImportSharedStep(ProjectId, NewSectionId, Arg.Any<SharedStep>())
            .Returns(newSharedStepId);

        // Act
        var result = await _sharedStepService.ImportSharedSteps(
            ProjectId,
            new[] { sharedStepId },
            sectionsMap,
            attributesMap
        );

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[sharedStepId], Is.EqualTo(newSharedStepId));

            await _parserService.Received(1).GetSharedStep(sharedStepId);
            await _attachmentService.Received(1).GetAttachments(sharedStepId, Arg.Any<IEnumerable<string>>());

            await _clientAdapter.Received(1).ImportSharedStep(
                ProjectId,
                NewSectionId,
                Arg.Is<SharedStep>(s =>
                    s.Attributes.Any() &&
                    s.Attachments.Contains(attachmentId.ToString())
                )
            );
        });
    }

    [Test]
    public async Task ImportSharedSteps_WhenMultipleSharedSteps_ImportsAllSuccessfully()
    {
        // Arrange
        var sharedStep1Id = Guid.NewGuid();
        var sharedStep2Id = Guid.NewGuid();
        var newSharedStep1Id = Guid.NewGuid();
        var newSharedStep2Id = Guid.NewGuid();

        var sharedStep1 = new SharedStep
        {
            Id = sharedStep1Id,
            Name = "Shared Step 1",
            SectionId = SectionId,
            Steps = new List<Step> { new() { Action = "Action 1" } }
        };

        var sharedStep2 = new SharedStep
        {
            Id = sharedStep2Id,
            Name = "Shared Step 2",
            SectionId = SectionId,
            Steps = new List<Step> { new() { Action = "Action 2" } }
        };

        var sectionsMap = new Dictionary<Guid, Guid> { { SectionId, NewSectionId } };

        _parserService.GetSharedStep(sharedStep1Id).Returns(sharedStep1);
        _parserService.GetSharedStep(sharedStep2Id).Returns(sharedStep2);
        _attachmentService.GetAttachments(Arg.Any<Guid>(), Arg.Any<IEnumerable<string>>())
            .Returns(new Dictionary<string, Guid>());
        _clientAdapter.ImportSharedStep(ProjectId, NewSectionId, Arg.Is<SharedStep>(s => s.Id == sharedStep1Id))
            .Returns(newSharedStep1Id);
        _clientAdapter.ImportSharedStep(ProjectId, NewSectionId, Arg.Is<SharedStep>(s => s.Id == sharedStep2Id))
            .Returns(newSharedStep2Id);

        // Act
        var result = await _sharedStepService.ImportSharedSteps(
            ProjectId,
            new[] { sharedStep1Id, sharedStep2Id },
            sectionsMap,
            new Dictionary<Guid, TmsAttribute>()
        );

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[sharedStep1Id], Is.EqualTo(newSharedStep1Id));
            Assert.That(result[sharedStep2Id], Is.EqualTo(newSharedStep2Id));

            await _parserService.Received(1).GetSharedStep(sharedStep1Id);
            await _parserService.Received(1).GetSharedStep(sharedStep2Id);
            await _attachmentService.Received(2).GetAttachments(Arg.Any<Guid>(), Arg.Any<IEnumerable<string>>());
            await _clientAdapter.Received(2).ImportSharedStep(ProjectId, NewSectionId, Arg.Any<SharedStep>());
        });
    }
}
