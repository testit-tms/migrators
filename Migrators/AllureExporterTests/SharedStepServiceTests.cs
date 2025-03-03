using AllureExporter.Client;
using AllureExporter.Models.Step;
using AllureExporter.Services;
using AllureExporter.Services.Implementations;
using Microsoft.Extensions.Logging;
using Models;
using Moq;
using Attribute = Models.Attribute;

namespace AllureExporterTests;

public class SharedStepServiceTests
{
    private Mock<ILogger<SharedStepService>> _logger;
    private Mock<IClient> _client;
    private Mock<IStepService> _stepService;
    private Mock<IAttachmentService> _attachmentService;
    private SharedStepService _sut;

    private const long ProjectId = 1;
    private readonly Guid _sectionId = Guid.NewGuid();
    private readonly List<Attribute> _attributes = new()
    {
        new Attribute { Id = Guid.NewGuid(), Name = "CustomField1" },
        new Attribute { Id = Guid.NewGuid(), Name = "CustomField2" }
    };

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<SharedStepService>>();
        _client = new Mock<IClient>();
        _stepService = new Mock<IStepService>();
        _attachmentService = new Mock<IAttachmentService>();
        _sut = new SharedStepService(_logger.Object, _client.Object, _stepService.Object, _attachmentService.Object);
    }

    [Test]
    public async Task ConvertSharedSteps_FailedGetSharedSteps()
    {
        // Arrange
        _client.Setup(x => x.GetSharedStepsByProjectId(ProjectId))
            .ReturnsAsync((List<AllureSharedStep>)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NullReferenceException>(() => _sut.ConvertSharedSteps(ProjectId, _sectionId, _attributes));
    }

    [Test]
    public async Task ConvertSharedSteps_FailedGetStepsInfo()
    {
        // Arrange
        var sharedSteps = new List<AllureSharedStep>
        {
            new() { Id = 1, Name = "Shared Step 1" }
        };
        var expectedErrorMessage = "Failed to get steps info";

        _client.Setup(x => x.GetSharedStepsByProjectId(ProjectId))
            .ReturnsAsync(sharedSteps);
        _client.Setup(x => x.GetStepsInfoBySharedStepId(It.IsAny<long>()))
            .ThrowsAsync(new Exception(expectedErrorMessage));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(() => _sut.ConvertSharedSteps(ProjectId, _sectionId, _attributes));
        Assert.That(ex.Message, Is.EqualTo(expectedErrorMessage));
    }

    [Test]
    public async Task ConvertSharedSteps_Success()
    {
        // Arrange
        var sharedSteps = new List<AllureSharedStep>
        {
            new() { Id = 1, Name = "Shared Step 1" },
            new() { Id = 2, Name = "Shared Step 2" }
        };

        var stepsInfo = new AllureSharedStepsInfo
        {
            Root = new AllureScenarioRoot { NestedStepIds = new List<long> { 1, 2 } },
            SharedStepScenarioStepsDictionary = new Dictionary<string, AllureScenarioStep>
            {
                { "1", new AllureScenarioStep { Id = 1, Body = "Step 1" } },
                { "2", new AllureScenarioStep { Id = 2, Body = "Step 2" } }
            }
        };

        var convertedSteps = new List<Step>
        {
            new() { Action = "Step 1", Expected = "Expected 1" },
            new() { Action = "Step 2", Expected = "Expected 2" }
        };

        _client.Setup(x => x.GetSharedStepsByProjectId(ProjectId))
            .ReturnsAsync(sharedSteps);
        _client.Setup(x => x.GetStepsInfoBySharedStepId(It.IsAny<long>()))
            .ReturnsAsync(stepsInfo);
        _stepService.Setup(x => x.ConvertStepsForSharedStep(It.IsAny<long>()))
            .ReturnsAsync(convertedSteps);
        _attachmentService.Setup(x => x.DownloadAttachmentsforSharedStep(It.IsAny<long>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<string> { "attachment1.png", "attachment2.png" });

        // Act
        var result = await _sut.ConvertSharedSteps(ProjectId, _sectionId, _attributes);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));

            // Verify first shared step
            var firstStep = result[1];
            Assert.That(firstStep.Name, Is.EqualTo("Shared Step 1"));
            Assert.That(firstStep.Steps, Has.Count.EqualTo(2));
            Assert.That(firstStep.Attachments, Has.Count.EqualTo(2));
            Assert.That(firstStep.Attributes, Has.Count.EqualTo(2));
            Assert.That(firstStep.SectionId, Is.EqualTo(_sectionId));
            Assert.That(firstStep.State, Is.EqualTo(StateType.NotReady));
            Assert.That(firstStep.Priority, Is.EqualTo(PriorityType.Medium));

            // Verify second shared step
            var secondStep = result[2];
            Assert.That(secondStep.Name, Is.EqualTo("Shared Step 2"));
            Assert.That(secondStep.Steps, Has.Count.EqualTo(2));
            Assert.That(secondStep.Attachments, Has.Count.EqualTo(2));
            Assert.That(secondStep.Attributes, Has.Count.EqualTo(2));
            Assert.That(secondStep.SectionId, Is.EqualTo(_sectionId));
            Assert.That(secondStep.State, Is.EqualTo(StateType.NotReady));
            Assert.That(secondStep.Priority, Is.EqualTo(PriorityType.Medium));
        });

        _client.Verify(x => x.GetSharedStepsByProjectId(ProjectId), Times.Once);
        _client.Verify(x => x.GetStepsInfoBySharedStepId(It.IsAny<long>()), Times.Exactly(2));
        _stepService.Verify(x => x.ConvertStepsForSharedStep(It.IsAny<long>()), Times.Exactly(2));
        _attachmentService.Verify(x => x.DownloadAttachmentsforSharedStep(It.IsAny<long>(), It.IsAny<Guid>()), Times.Exactly(2));
    }
}
