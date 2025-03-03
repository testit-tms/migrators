using AllureExporter.Client;
using AllureExporter.Models.Attachment;
using AllureExporter.Models.Step;
using AllureExporter.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;

namespace AllureExporterTests;

public class StepServiceTests
{
    private Mock<ILogger<StepService>> _logger;
    private Mock<IClient> _client;
    private StepService _sut;
    private const long TestCaseId = 1;
    private List<AllureStep> _allureSteps;
    private List<AllureAttachment> _commonAttachments;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<StepService>>();
        _client = new Mock<IClient>();
        _sut = new StepService(_logger.Object, _client.Object);

        _commonAttachments = new List<AllureAttachment>
        {
            new() { Id = 1, Name = "image.png" },
            new() { Id = 2, Name = "image2.png" },
            new() { Id = 3, Name = "image3.png" }
        };

        _allureSteps = new List<AllureStep>
        {
            new()
            {
                Keyword = "When",
                Name = "Test step 1",
                ExpectedResult = "Expected result",
                Attachments = new List<AllureAttachment>
                {
                    new() { Id = 1, Name = "image.png" },
                    new() { Id = 2, Name = "image2.png" }
                },
                Steps = new List<AllureStep>
                {
                    new()
                    {
                        Name = "Test step 1.1",
                        ExpectedResult = "Expected result 1.1",
                        Attachments = new List<AllureAttachment>
                        {
                            new() { Id = 3, Name = "image3.png" }
                        },
                    },
                    new()
                    {
                        Keyword = "And",
                        Name = "Test step 1.2",
                        ExpectedResult = "Expected result 1.2",
                        Attachments = new List<AllureAttachment>()
                    }
                }
            },
            new()
            {
                Name = string.Empty,
                Steps = new List<AllureStep>(),
                Attachments = new List<AllureAttachment>()
            },
            new()
            {
                Name = "Test step 3",
                Steps = new List<AllureStep>(),
                Attachments = new List<AllureAttachment>()
            }
        };
    }

    [Test]
    public async Task ConvertSteps_FailedGetSteps()
    {
        // Arrange
        _client.Setup(x => x.GetSteps(It.IsAny<long>()))
            .ReturnsAsync((List<AllureStep>)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ConvertStepsForTestCase(1, new Dictionary<string, Guid>()));
        Assert.That(ex.ParamName, Is.EqualTo("source"));
    }

    [Test]
    public async Task ConvertSteps_GetStepsSuccess()
    {
        // Arrange
        _client.Setup(x => x.GetSteps(It.IsAny<long>()))
            .ReturnsAsync(_allureSteps);
        _client.Setup(x => x.GetAttachmentsByTestCaseId(It.IsAny<long>()))
            .ReturnsAsync(_commonAttachments);

        // Act
        var steps = await _sut.ConvertStepsForTestCase(TestCaseId, new Dictionary<string, Guid>());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(steps, Has.Count.EqualTo(3));

            // Verify first step
            var expectedAction = "<p>When</p>\r\n<p>Test step 1</p>\r\n<p>Test step 1.1</p>\r\n<p>And</p>\r\n<p>Test step 1.2</p>\r\n";
            Assert.That(steps[0].Action, Is.EqualTo(expectedAction));
            Assert.That(steps[0].Expected, Is.EqualTo("Expected result"));
            Assert.That(steps[0].ActionAttachments, Has.Count.EqualTo(3));
            Assert.That(steps[0].ActionAttachments.ToList(), Is.EqualTo(new List<string> { "image.png", "image2.png", "image3.png" }));

            // Verify second step
            Assert.That(steps[1].Action, Is.EqualTo("<p></p>\r\n"));
            Assert.That(steps[1].Expected, Is.Empty);
            Assert.That(steps[1].ActionAttachments, Is.Empty);

            // Verify third step
            Assert.That(steps[2].Action, Is.EqualTo("<p>Test step 3</p>\r\n"));
            Assert.That(steps[2].Expected, Is.Empty);
            Assert.That(steps[2].ActionAttachments, Is.Empty);
        });

        _client.Verify(x => x.GetSteps(TestCaseId), Times.Once);
        _client.Verify(x => x.GetAttachmentsByTestCaseId(TestCaseId), Times.Once);
    }

    [Test]
    public async Task ConvertSteps_GetStepsInfoSuccess()
    {
        // Arrange
        _client.Setup(x => x.GetSteps(It.IsAny<long>()))
            .ReturnsAsync(new List<AllureStep>());
        _client.Setup(x => x.GetAttachmentsByTestCaseId(It.IsAny<long>()))
            .ReturnsAsync(new List<AllureAttachment>());
        _client.Setup(x => x.GetStepsInfoByTestCaseId(It.IsAny<long>()))
            .ReturnsAsync(new AllureStepsInfo
            {
                Root = new AllureScenarioRoot
                {
                    NestedStepIds = new List<long> { 1, 2, 3 }
                },
                ScenarioStepsDictionary = new Dictionary<string, AllureScenarioStep>
                {
                    {
                        "1", new AllureScenarioStep
                        {
                            Id = 1,
                            Body = "Step 1",
                            ExpectedResult = "Expected 1"
                        }
                    },
                    {
                        "2", new AllureScenarioStep
                        {
                            Id = 2,
                            Body = "Step 2",
                            ExpectedResult = "Expected 2"
                        }
                    },
                    {
                        "3", new AllureScenarioStep
                        {
                            Id = 3,
                            Body = "Step 3",
                            ExpectedResult = "Expected 3"
                        }
                    }
                }
            });

        // Act
        var steps = await _sut.ConvertStepsForTestCase(TestCaseId, new Dictionary<string, Guid>());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(steps, Has.Count.EqualTo(3));

            // Verify first step
            Assert.That(steps[0].Action, Is.EqualTo("Step 1"));
            Assert.That(steps[0].Expected, Is.EqualTo("Expected 1"));
            Assert.That(steps[0].ActionAttachments, Is.Empty);

            // Verify second step
            Assert.That(steps[1].Action, Is.EqualTo("Step 2"));
            Assert.That(steps[1].Expected, Is.EqualTo("Expected 2"));
            Assert.That(steps[1].ActionAttachments, Is.Empty);

            // Verify third step
            Assert.That(steps[2].Action, Is.EqualTo("Step 3"));
            Assert.That(steps[2].Expected, Is.EqualTo("Expected 3"));
            Assert.That(steps[2].ActionAttachments, Is.Empty);
        });

        _client.Verify(x => x.GetSteps(TestCaseId), Times.Once);
        _client.Verify(x => x.GetAttachmentsByTestCaseId(TestCaseId), Times.Once);
        _client.Verify(x => x.GetStepsInfoByTestCaseId(TestCaseId), Times.Once);
    }
}
