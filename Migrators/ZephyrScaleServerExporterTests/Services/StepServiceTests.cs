using Microsoft.Extensions.Logging;
using Models;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Helpers;
using ZephyrScaleServerExporter.Services.Implementations;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class StepServiceTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<IAttachmentService> _mockAttachmentService;
    private Mock<ILogger<StepService>> _mockLogger;
    private Mock<IParameterService> _mockParameterService;
    private Mock<IClient> _mockClient;
    private StepService _stepService;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockAttachmentService = new Mock<IAttachmentService>();
        _mockLogger = new Mock<ILogger<StepService>>();
        _mockParameterService = new Mock<IParameterService>();
        _mockClient = new Mock<IClient>();

        _stepService = new StepService(
            _mockDetailedLogService.Object,
            _mockAttachmentService.Object,
            _mockLogger.Object,
            _mockParameterService.Object,
            _mockClient.Object);
    }

    #region ConvertSteps

    [Test]
    public async Task ConvertSteps_WithStepBasedScript_ProcessesSteps()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new() { Index = 2, Description = "Step 2" },
                new() { Index = 1, Description = "Step 1" }
            }
        };
        var iterations = new List<Iteration>();

        _mockAttachmentService.Setup(x => x.DownloadAttachmentById(testCaseId, It.IsAny<StepAttachment>(), true))
            .ReturnsAsync("attachment.txt");

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(2));

            // Should be ordered by index
            Assert.That(result.Steps[0].Action, Contains.Substring("Step 1"));
            Assert.That(result.Steps[1].Action, Contains.Substring("Step 2"));
            Assert.That(result.Iterations, Is.EqualTo(iterations));
        });
    }

    [Test]
    public async Task ConvertSteps_WithTextBasedScript_HandlesText()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Text = "This is a text script\nWith multiple lines"
        };
        var iterations = new List<Iteration>();

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].Action, Contains.Substring("This is a text script"));
            Assert.That(result.Steps[0].Action, Contains.Substring("With multiple lines"));
            Assert.That(result.Steps[0].Expected, Is.Empty);
            Assert.That(result.Steps[0].TestData, Is.Empty);
            Assert.That(result.Iterations, Is.EqualTo(iterations));
        });
    }

    [Test]
    public async Task ConvertSteps_WithNullStepsAndText_ReturnsEmptySteps()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript();
        var iterations = new List<Iteration>();

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.Iterations, Is.EqualTo(iterations));
        });
    }

    [Test]
    public async Task ConvertSteps_WithStepContainingAttachments_DownloadsAttachments()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var attachmentId = 123;
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "Step with attachment",
                    Attachments = new List<StepAttachment?>
                    {
                        new() { Id = attachmentId }
                    }
                }
            }
        };
        var iterations = new List<Iteration>();

        _mockAttachmentService.Setup(x => x.DownloadAttachmentById(testCaseId, It.Is<StepAttachment>(a => a.Id == attachmentId), true))
            .ReturnsAsync("downloaded_attachment.txt");

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].Attachments, Contains.Item("downloaded_attachment.txt"));
        });
    }

    [Test]
    public async Task ConvertSteps_WithStepContainingSharedSteps_ResolvesSharedSteps()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var sharedTestCaseKey = "SHARED-1";
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    TestCaseKey = sharedTestCaseKey
                }
            }
        };
        var iterations = new List<Iteration>();

        // Setup parameter service
        _mockParameterService.Setup(x => x.ConvertParameters(sharedTestCaseKey))
            .ReturnsAsync(new List<Iteration>());

        _mockParameterService.Setup(x => x.MergeIterations(iterations, It.IsAny<List<Iteration>>()))
            .Returns(iterations);

        // Setup client to return shared test case
        _mockClient.Setup(x => x.GetTestCase(sharedTestCaseKey))
            .ReturnsAsync(new ZephyrTestCase
            {
                Key = sharedTestCaseKey,
                TestScript = new ZephyrTestScript
                {
                    Steps = new List<ZephyrStep>
                    {
                        new() { Index = 1, Description = "Shared step 1" }
                    }
                },
                Parameters = new Dictionary<string, object>() // Required property
            });

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.AtLeast(1));

            // Verify that the shared step prefix was added
            Assert.That(result.Steps[0].Action, Does.StartWith("1.1 Общий шаг SHARED-1"));
            Assert.That(result.Steps[0].Action, Contains.Substring("Shared step 1"));
        });

        // Verify interactions
        _mockClient.Verify(x => x.GetTestCase(sharedTestCaseKey), Times.Once);
        _mockDetailedLogService.Verify(x => x.LogInformation(It.IsAny<string>(), It.IsAny<object>()), Times.AtLeastOnce());

        // Verify that log was called with the prefix string
        _mockDetailedLogService.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("Общий шаг")), It.IsAny<object[]>()), Times.AtLeastOnce());
    }

    [Test]
    public async Task ConvertSteps_WithStepContainingCustomFields_AddsToTestData()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "Test step",
                    ExpectedResult = "Expected result",
                    TestData = "",
                    CustomFields = new List<ZephyrCustomField>
                    {
                        new()
                        {
                            CustomField = new ZephyrCustomFieldData { Name = "Test Field" },
                            StringValue = "Test Value"
                        }
                    }
                }
            }
        };
        var iterations = new List<Iteration>();

        _mockAttachmentService.Setup(x => x.DownloadAttachmentById(testCaseId, It.IsAny<StepAttachment>(), true))
            .ReturnsAsync("attachment.txt");

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].TestData, Does.Contain("Test Field: Test Value"));
        });
    }

    [Test]
    public async Task ConvertSteps_WithParametersInStepText_ReplacesParameters()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "<span class=\"atwho-inserted\" data-atwho-at-query=\"{\">{param1}</span>"
                }
            }
        };
        var iterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param1", Value = "replacement_value" }
                }
            }
        };

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].Action, Does.Contain("<<<param1>>>"));
            Assert.That(result.Steps[0].Action, Does.Not.Contain("<span class=\"atwho-inserted\" data-atwho-at-query=\"{\">{param1}</span>"));
        });
    }

    [Test]
    public void ConvertSteps_WithExceptionInProcessing_ThrowsException()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    TestCaseKey = "ERROR-1"
                }
            }
        };
        var iterations = new List<Iteration>();

        // Setup to throw exception when processing shared steps
        _mockParameterService.Setup(x => x.ConvertParameters("ERROR-1"))
            .Throws(new Exception("Test exception"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () =>
            await _stepService.ConvertSteps(testCaseId, testScript, iterations));
    }

    [Test]
    public async Task ConvertSteps_WithCustomFieldOptions_IntValue_HandlesCorrectly()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "Test step",
                    ExpectedResult = "Expected result",
                    TestData = "",
                    CustomFields = new List<ZephyrCustomField>
                    {
                        new()
                        {
                            CustomField = new ZephyrCustomFieldData
                            {
                                Name = "Priority Field",
                                Options = new List<ZephyrCustomFieldOption>
                                {
                                    new() { Id = 1, Name = "High" },
                                    new() { Id = 2, Name = "Medium" },
                                    new() { Id = 3, Name = "Low" }
                                }
                            },
                            IntValue = 1
                        }
                    }
                }
            }
        };

        var iterations = new List<Iteration>();

        _mockAttachmentService.Setup(x => x.DownloadAttachmentById(testCaseId, It.IsAny<StepAttachment>(), true))
            .ReturnsAsync("attachment.txt");

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].TestData, Does.Contain("Priority Field: High"));
        });
    }

    [Test]
    public async Task ConvertSteps_WithCustomFieldOptions_StringValue_HandlesCorrectly()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "Test step",
                    ExpectedResult = "Expected result",
                    TestData = "",
                    CustomFields = new List<ZephyrCustomField>
                    {
                        new()
                        {
                            CustomField = new ZephyrCustomFieldData
                            {
                                Name = "Tags Field",
                                Options = new List<ZephyrCustomFieldOption>
                                {
                                    new() { Id = 1, Name = "UI" },
                                    new() { Id = 2, Name = "API" },
                                    new() { Id = 3, Name = "DB" }
                                }
                            },
                            StringValue = "[1,2]"
                        }
                    }
                }
            }
        };

        var iterations = new List<Iteration>();

        _mockAttachmentService.Setup(x => x.DownloadAttachmentById(testCaseId, It.IsAny<StepAttachment>(), true))
            .ReturnsAsync("attachment.txt");

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].TestData, Does.Contain("Tags Field:"));
            Assert.That(result.Steps[0].TestData, Does.Contain("UI,"));
            Assert.That(result.Steps[0].TestData, Does.Contain("API,"));
        });
    }

    [Test]
    public async Task ConvertSteps_WithArchivedTestCase_HandlesArchivePath()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var sharedTestCaseKey = "ARCHIVED-1";
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    TestCaseKey = sharedTestCaseKey
                }
            }
        };
        var iterations = new List<Iteration>();

        // Setup parameter service to work normally
        _mockParameterService.Setup(x => x.ConvertParameters(sharedTestCaseKey))
            .ReturnsAsync(new List<Iteration>());

        _mockParameterService.Setup(x => x.MergeIterations(iterations, It.IsAny<List<Iteration>>()))
            .Returns(iterations);

        // Setup client to throw on normal get but return archived test case
        _mockClient.Setup(x => x.GetTestCase(sharedTestCaseKey))
            .Throws(new Exception("Not found"));

        _mockClient.Setup(x => x.GetArchivedTestCase(sharedTestCaseKey))
            .ReturnsAsync(new ZephyrArchivedTestCase
            {
                Key = sharedTestCaseKey,
                TestScript = new ZephyrArchivedTestScript
                {
                    StepScript = new ZephyrStepByStepScript
                    {
                        Steps = new List<ZephyrStep>
                        {
                            new() { Index = 1, Description = "Archived step 1" }
                        }
                    }
                }
            });

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.AtLeast(1));
            Assert.That(result.Steps[0].Action, Contains.Substring("Archived step 1"));
        });

        // Verify interactions
        _mockClient.Verify(x => x.GetTestCase(sharedTestCaseKey), Times.Once);
        _mockClient.Verify(x => x.GetArchivedTestCase(sharedTestCaseKey), Times.Once);
    }

    [Test]
    public async Task ConvertSteps_EmptyStepsList_ReturnsEmptySteps()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>() // Empty list
        };
        var iterations = new List<Iteration>();

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.Iterations, Is.EqualTo(iterations));
        });
    }

    [Test]
    public async Task ConvertSteps_WithCachedSharedSteps_UsesCache()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var sharedTestCaseKey = "CACHED-1";
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    TestCaseKey = sharedTestCaseKey
                }
            }
        };
        var iterations = new List<Iteration>();

        // Setup counter to track calls
        var getClientCallCount = 0;

        // First call to populate cache - this simulates that the shared step was already processed
        _mockParameterService.Setup(x => x.ConvertParameters(sharedTestCaseKey))
            .ReturnsAsync(new List<Iteration>());

        _mockParameterService.Setup(x => x.MergeIterations(iterations, It.IsAny<List<Iteration>>()))
            .Returns(iterations);

        _mockClient.Setup(x => x.GetTestCase(sharedTestCaseKey))
            .Returns(() => {
                getClientCallCount++;
                return Task.FromResult(new ZephyrTestCase
                {
                    Key = sharedTestCaseKey,
                    TestScript = new ZephyrTestScript
                    {
                        Steps = new List<ZephyrStep>
                        {
                            new() { Index = 1, Description = "Cached step 1" }
                        }
                    },
                    Parameters = new Dictionary<string, object>()
                });
            });

        _mockAttachmentService.Setup(x => x.CopySharedAttachments(testCaseId, It.IsAny<Step>()))
            .Returns(Task.FromResult(new List<string> { "copied_attachment.txt" }));

        // Make first call to populate cache
        await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Verify first call happened
        Assert.That(getClientCallCount, Is.EqualTo(1));

        // Reset counter for second call
        getClientCallCount = 0;

        // Act - second call should use cache
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.AtLeast(1));
        });

        // Verify that GetTestCase was NOT called during second call (cache was used)
        Assert.That(getClientCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ConvertSteps_WithStepContainingEmptyAttachments_HandlesGracefully()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "Step with empty attachments",
                    Attachments = new List<StepAttachment?>() // Empty list
                }
            }
        };
        var iterations = new List<Iteration>();

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].Attachments, Is.Empty);
        });
    }

    [Test]
    public async Task ConvertSteps_WithSharedStepsException_LogsWarningAndContinues()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var sharedTestCaseKey = "SHARED-EXCEPTION";
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    TestCaseKey = sharedTestCaseKey
                }
            }
        };
        var iterations = new List<Iteration>();

        // Setup parameter service
        _mockParameterService.Setup(x => x.ConvertParameters(sharedTestCaseKey))
            .ReturnsAsync(new List<Iteration>());

        _mockParameterService.Setup(x => x.MergeIterations(iterations, It.IsAny<List<Iteration>>()))
            .Returns(iterations);

        // Setup client to return shared test case
        _mockClient.Setup(x => x.GetTestCase(sharedTestCaseKey))
            .ReturnsAsync(new ZephyrTestCase
            {
                Key = sharedTestCaseKey,
                TestScript = new ZephyrTestScript
                {
                    Steps = new List<ZephyrStep>
                    {
                        new() { Index = 1, Description = "Shared step 1" }
                    }
                },
                Parameters = new Dictionary<string, object>()
            });

        // Setup client to throw exception when getting attachments
        _mockClient.Setup(x => x.GetAttachmentsForTestCase(sharedTestCaseKey))
            .Throws(new Exception("Network error"));

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.AtLeast(1));
        });

        // Verify that warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get shared step attachment")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ConvertSteps_WithSharedSteps_CallsGetAttachmentsForTestCase()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var sharedTestCaseKey = "SHARED-WITH-ATTACHMENTS";
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    TestCaseKey = sharedTestCaseKey
                }
            }
        };
        var iterations = new List<Iteration>();

        // Setup parameter service
        _mockParameterService.Setup(x => x.ConvertParameters(sharedTestCaseKey))
            .ReturnsAsync(new List<Iteration>());

        _mockParameterService.Setup(x => x.MergeIterations(iterations, It.IsAny<List<Iteration>>()))
            .Returns(iterations);

        // Setup client to return shared test case
        _mockClient.Setup(x => x.GetTestCase(sharedTestCaseKey))
            .ReturnsAsync(new ZephyrTestCase
            {
                Key = sharedTestCaseKey,
                TestScript = new ZephyrTestScript
                {
                    Steps = new List<ZephyrStep>
                    {
                        new() { Index = 1, Description = "Shared step" }
                    }
                },
                Parameters = new Dictionary<string, object>()
            });

        // Setup client to return attachments for the shared test case
        _mockClient.Setup(x => x.GetAttachmentsForTestCase(sharedTestCaseKey))
            .ReturnsAsync(new List<ZephyrAttachment>());

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.AtLeast(1));
        });

        // Verify that GetAttachmentsForTestCase was called
        _mockClient.Verify(x => x.GetAttachmentsForTestCase(sharedTestCaseKey), Times.Once);
    }

    [Test]
    public async Task ConvertSteps_WithParametersInMultipleIterations_ReplacesAllParameters()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "<span class=\"atwho-inserted\" data-atwho-at-query=\"{\">{param1}</span> and " +
                                 "<span class=\"atwho-inserted\" data-atwho-at-query=\"{\">{param2}</span>"
                }
            }
        };

        var iterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param1", Value = "value1" }
                }
            },
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param2", Value = "value2" }
                }
            }
        };

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].Action, Does.Contain("<<<param1>>>"));
            Assert.That(result.Steps[0].Action, Does.Contain("<<<param2>>>"));
            Assert.That(result.Steps[0].Action, Does.Not.Contain("<span class=\"atwho-inserted\" data-atwho-at-query=\"{\">{param1}</span>"));
            Assert.That(result.Steps[0].Action, Does.Not.Contain("<span class=\"atwho-inserted\" data-atwho-at-query=\"{\">{param2}</span>"));
        });
    }

    [Test]
    public async Task ConvertSteps_WithEmptyCustomFields_HandlesGracefully()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "Test step",
                    ExpectedResult = "Expected result",
                    TestData = "Original test data",
                    CustomFields = new List<ZephyrCustomField>() // Empty list
                }
            }
        };
        var iterations = new List<Iteration>();

        _mockAttachmentService.Setup(x => x.DownloadAttachmentById(testCaseId, It.IsAny<StepAttachment>(), true))
            .ReturnsAsync("attachment.txt");

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].TestData, Is.EqualTo("Original test data"));
        });
    }

    [Test]
    public async Task ConvertSteps_WithNullCustomFieldValue_HandlesGracefully()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "Test step",
                    ExpectedResult = "Expected result",
                    TestData = "Original test data",
                    CustomFields = new List<ZephyrCustomField>
                    {
                        new()
                        {
                            CustomField = new ZephyrCustomFieldData
                            {
                                Name = "Empty Field",
                                Options = null
                            },
                            StringValue = null,
                            IntValue = null
                        }
                    }
                }
            }
        };
        var iterations = new List<Iteration>();

        _mockAttachmentService.Setup(x => x.DownloadAttachmentById(testCaseId, It.IsAny<StepAttachment>(), true))
            .ReturnsAsync("attachment.txt");

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].TestData, Is.EqualTo("Original test data"));
        });
    }

    [Test]
    public async Task ConvertSteps_WithCustomFieldStringIds_ParsesMultipleIdsCorrectly()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testScript = new ZephyrTestScript
        {
            Steps = new List<ZephyrStep>
            {
                new()
                {
                    Index = 1,
                    Description = "Test step",
                    ExpectedResult = "Expected result",
                    TestData = "",
                    CustomFields = new List<ZephyrCustomField>
                    {
                        new()
                        {
                            CustomField = new ZephyrCustomFieldData
                            {
                                Name = "Multi-value Field",
                                Options = new List<ZephyrCustomFieldOption>
                                {
                                    new() { Id = 10, Name = "Option A" },
                                    new() { Id = 20, Name = "Option B" },
                                    new() { Id = 30, Name = "Option C" }
                                }
                            },
                            StringValue = "[10,20,30]" // Multiple IDs in brackets
                        }
                    }
                }
            }
        };
        var iterations = new List<Iteration>();

        _mockAttachmentService.Setup(x => x.DownloadAttachmentById(testCaseId, It.IsAny<StepAttachment>(), true))
            .ReturnsAsync("attachment.txt");

        // Act
        var result = await _stepService.ConvertSteps(testCaseId, testScript, iterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Steps, Has.Count.EqualTo(1));
            Assert.That(result.Steps[0].TestData, Does.Contain("Multi-value Field:"));
            Assert.That(result.Steps[0].TestData, Does.Contain("Option A,"));
            Assert.That(result.Steps[0].TestData, Does.Contain("Option B,"));
            Assert.That(result.Steps[0].TestData, Does.Contain("Option C,"));
        });
    }
    #endregion
}
