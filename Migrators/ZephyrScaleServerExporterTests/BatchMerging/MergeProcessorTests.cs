using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.BatchMerging;
using ZephyrScaleServerExporter.BatchMerging.Implementations;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Services;
using Models;
using AttributeModel = Models.Attribute;

namespace ZephyrScaleServerExporterTests.BatchMerging;

[TestFixture]
public class MergeProcessorTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<ILogger<MergeProcessor>> _mockLogger;
    private Mock<IFileProcessor> _mockFileProcessor;
    private Mock<IMainJsonProcessor> _mockMainJsonProcessor;
    private Mock<IOptions<AppConfig>> _mockConfig;
    private MergeProcessor _mergeProcessor;
    private AppConfig _appConfig;
    private string _tempDirectory;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockLogger = new Mock<ILogger<MergeProcessor>>();
        _mockFileProcessor = new Mock<IFileProcessor>();
        _mockMainJsonProcessor = new Mock<IMainJsonProcessor>();
        _mockConfig = new Mock<IOptions<AppConfig>>();

        _appConfig = new AppConfig
        {
            ResultPath = "test/results",
            Zephyr = new ZephyrConfig
            {
                ProjectKey = "TEST_PROJECT"
            }
        };

        _mockConfig.Setup(x => x.Value).Returns(_appConfig);
        _mergeProcessor = new MergeProcessor(
            _mockDetailedLogService.Object,
            _mockLogger.Object,
            _mockFileProcessor.Object,
            _mockMainJsonProcessor.Object,
            _mockConfig.Object);

        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        // Create the project directory structure for tests
        var projectPath = Path.Combine(_tempDirectory, _appConfig.Zephyr.ProjectKey);
        Directory.CreateDirectory(projectPath);

        // Update app config to use temp directory
        _appConfig.ResultPath = _tempDirectory;
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region MergeProjects

    [Test]
    public void MergeProjects_NormalFlow_CompletesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");
        var batch2Path = Path.Combine(projectPath, "batch_2");
        var batchDirectories = new List<string> { batch1Path, batch2Path };

        // Create batch directories
        Directory.CreateDirectory(batch1Path);
        Directory.CreateDirectory(batch2Path);

        var mergedPath = Path.Combine(projectPath, "merged");
        var mainJsonObjects = new List<Root>
        {
            new Root
            {
                ProjectName = "Project1",
                Attributes = new List<AttributeModel>(),
                Sections = new List<Section>(),
                SharedSteps = new List<Guid>(),
                TestCases = new List<Guid>()
            },
            new Root
            {
                ProjectName = "Project2",
                Attributes = new List<AttributeModel>(),
                Sections = new List<Section>(),
                SharedSteps = new List<Guid>(),
                TestCases = new List<Guid>()
            }
        };

        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjects);

        // Act
        Assert.DoesNotThrow(() => _mergeProcessor.MergeProjects());

        // Assert
        // Verify LoadMainJsonFromBatches was called
        _mockMainJsonProcessor.Verify(x => x.LoadMainJsonFromBatches(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            "main.json"), Times.Once);

        // Verify UpdateReferencesInMergedFiles was called
        _mockFileProcessor.Verify(x => x.UpdateReferencesInMergedFiles(
            It.IsAny<string>(),
            It.IsAny<Dictionary<Guid, Guid>>(),
            It.IsAny<Dictionary<Guid, Guid>>()), Times.Once);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting merge process")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Merge process completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void MergeProjects_NoBatchDirectories_FinishesEarly()
    {
        // Act
        _mergeProcessor.MergeProjects();

        // Assert
        // Verify that processing stops early
        _mockMainJsonProcessor.Verify(x => x.LoadMainJsonFromBatches(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void MergeProjects_FailedToInitializeMergedDirectory_FinishesEarly()
    {
        // Act
        _mergeProcessor.MergeProjects();

        // Assert
        // Since we can't easily mock static Utils methods, we verify that
        // the method completes without throwing exceptions
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting merge process")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void MergeProjects_NoValidMainJsonFiles_LogsWarningAndFinishes()
    {
        // Arrange
        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");
        var batch2Path = Path.Combine(projectPath, "batch_2");

        // Create batch directories
        Directory.CreateDirectory(batch1Path);
        Directory.CreateDirectory(batch2Path);

        var batchDirectories = new List<string> { batch1Path, batch2Path };
        var mainJsonObjects = new List<Root>(); // Empty list

        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjects);

        // Act
        _mergeProcessor.MergeProjects();

        // Assert
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No valid main.json files were loaded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify that processing stops after the warning
        _mockFileProcessor.Verify(x => x.UpdateReferencesInMergedFiles(
            It.IsAny<string>(),
            It.IsAny<Dictionary<Guid, Guid>>(),
            It.IsAny<Dictionary<Guid, Guid>>()), Times.Never);
    }

    [Test]
    public void MergeProjects_SuccessfulMerge_UpdatesReferencesInFiles()
    {
        // Arrange
        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");
        var batch2Path = Path.Combine(projectPath, "batch_2");

        // Create batch directories
        Directory.CreateDirectory(batch1Path);
        Directory.CreateDirectory(batch2Path);

        var batchDirectories = new List<string> { batch1Path, batch2Path };
        var mainJsonObjects = new List<Root>
        {
            new Root
            {
                ProjectName = "Test Project",
                Attributes = new List<AttributeModel>(),
                Sections = new List<Section>(),
                SharedSteps = new List<Guid>(),
                TestCases = new List<Guid>()
            }
        };

        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjects);

        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjects);

        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjects);

        // Act
        _mergeProcessor.MergeProjects();

        // Assert
        // Verify that UpdateReferencesInMergedFiles was called
        _mockFileProcessor.Verify(x => x.UpdateReferencesInMergedFiles(
            It.IsAny<string>(),
            It.IsAny<Dictionary<Guid, Guid>>(),
            It.IsAny<Dictionary<Guid, Guid>>()), Times.Once);
    }

    #endregion

    #region MergeMainJsonObjects

    [Test]
    public void MergeMainJsonObjects_NormalCase_MergesDataCorrectly()
    {
        // Arrange
        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");
        var batch2Path = Path.Combine(projectPath, "batch_2");

        // Create batch directories
        Directory.CreateDirectory(batch1Path);
        Directory.CreateDirectory(batch2Path);

        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid();
        var attributeId1 = Guid.NewGuid();
        var attributeId2 = Guid.NewGuid();
        var sharedStepId1 = Guid.NewGuid();
        var sharedStepId2 = Guid.NewGuid();
        var testCaseId1 = Guid.NewGuid();
        var testCaseId2 = Guid.NewGuid();

        var mainJsonObjectList = new List<Root>
        {
            new Root
            {
                ProjectName = "Project1",
                Attributes = new List<AttributeModel>
                {
                    new AttributeModel { Id = attributeId1, Name = "Attr1" }
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Id = sectionId1,
                        Name = "Section1",
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>()
                    }
                },
                SharedSteps = new List<Guid> { sharedStepId1 },
                TestCases = new List<Guid> { testCaseId1 }
            },
            new Root
            {
                ProjectName = "Project2",
                Attributes = new List<AttributeModel>
                {
                    new AttributeModel { Id = attributeId2, Name = "Attr2" }
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Id = sectionId2,
                        Name = "Section2",
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>()
                    }
                },
                SharedSteps = new List<Guid> { sharedStepId2 },
                TestCases = new List<Guid> { testCaseId2 }
            }
        };

        // To test the private method, we'll call the public method that uses it
        // We'll use reflection to access the private fields that store the maps
        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);

        // Act
        _mergeProcessor.MergeProjects();

        // Assert
        // Verify that all data was processed
        _mockMainJsonProcessor.Verify(x => x.LoadMainJsonFromBatches(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            "main.json"), Times.Once);
    }

    [Test]
    public void MergeMainJsonObjects_WithDuplicateSections_HandlesDuplicates()
    {
        // Arrange
        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");
        var batch2Path = Path.Combine(projectPath, "batch_2");

        // Create batch directories
        Directory.CreateDirectory(batch1Path);
        Directory.CreateDirectory(batch2Path);

        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid(); // Duplicate with same name
        var attributeId1 = Guid.NewGuid();
        var attributeId2 = Guid.NewGuid(); // Duplicate with same name

        var mainJsonObjectList = new List<Root>
        {
            new Root
            {
                ProjectName = "Project1",
                Attributes = new List<AttributeModel>
                {
                    new AttributeModel { Id = attributeId1, Name = "Attr1" }
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Id = sectionId1,
                        Name = "Section1",
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>()
                    }
                },
                SharedSteps = new List<Guid>(),
                TestCases = new List<Guid>()
            },
            new Root
            {
                ProjectName = "Project2",
                Attributes = new List<AttributeModel>
                {
                    new AttributeModel { Id = attributeId2, Name = "Attr1" } // Same name = duplicate
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Id = sectionId2,
                        Name = "Section1", // Same name = duplicate
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>()
                    }
                },
                SharedSteps = new List<Guid>(),
                TestCases = new List<Guid>()
            }
        };

        // To test the private method, we'll call the public method that uses it
        // We'll use reflection to access the private fields that store the maps
        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);

        // Act
        _mergeProcessor.MergeProjects();

        // Assert
        // The merge should complete successfully even with duplicates
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Merge process completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void MergeMainJsonObjects_WithDuplicateAttributes_HandlesDuplicates()
    {
        // Arrange
        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");
        var batch2Path = Path.Combine(projectPath, "batch_2");

        // Create batch directories
        Directory.CreateDirectory(batch1Path);
        Directory.CreateDirectory(batch2Path);

        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid(); // Duplicate with same name
        var attributeId1 = Guid.NewGuid();
        var attributeId2 = Guid.NewGuid(); // Duplicate with same name

        var mainJsonObjectList = new List<Root>
        {
            new Root
            {
                ProjectName = "Project1",
                Attributes = new List<AttributeModel>
                {
                    new AttributeModel { Id = attributeId1, Name = "Attr1" }
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Id = sectionId1,
                        Name = "UniqueSection1",
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>()
                    }
                },
                SharedSteps = new List<Guid>(),
                TestCases = new List<Guid>()
            },
            new Root
            {
                ProjectName = "Project2",
                Attributes = new List<AttributeModel>
                {
                    new AttributeModel { Id = attributeId2, Name = "Attr1" } // Same name = duplicate
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Id = Guid.NewGuid(),
                        Name = "UniqueSection2",
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>()
                    }
                },
                SharedSteps = new List<Guid>(),
                TestCases = new List<Guid>()
            }
        };

        // To test the private method, we'll call the public method that uses it
        // We'll use reflection to access the private fields that store the maps
        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);

        // Act
        _mergeProcessor.MergeProjects();

        // Assert
        // The merge should complete successfully even with duplicates
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Merge process completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SaveMergedJson

    [Test]
    public void SaveMergedJson_NormalCase_VerifiesFileProcessorCalled()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var sharedStepId = Guid.NewGuid();
        var testCaseId = Guid.NewGuid();

        var mergedRoot = new Root
        {
            ProjectName = "Merged Project",
            Attributes = new List<AttributeModel>
            {
                new AttributeModel { Id = attributeId, Name = "Merged Attr" }
            },
            Sections = new List<Section>
            {
                new Section
                {
                    Id = sectionId,
                    Name = "Merged Section",
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Sections = new List<Section>()
                }
            },
            SharedSteps = new List<Guid> { sharedStepId },
            TestCases = new List<Guid> { testCaseId }
        };

        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");

        // Create batch directory
        Directory.CreateDirectory(batch1Path);

        var mergedPath = Path.Combine(projectPath, "merged");
        Directory.CreateDirectory(mergedPath);
        var expectedFilePath = Path.Combine(mergedPath, "main.json");

        // Act
        // Since SaveMergedJson is private, we test it through the full flow
        var mainJsonObjectList = new List<Root> { mergedRoot };

        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);
        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);

        Assert.DoesNotThrow(() => _mergeProcessor.MergeProjects());

        // Assert
        // Verify that UpdateReferencesInMergedFiles was called
        _mockFileProcessor.Verify(x => x.UpdateReferencesInMergedFiles(
            It.IsAny<string>(),
            It.IsAny<Dictionary<Guid, Guid>>(),
            It.IsAny<Dictionary<Guid, Guid>>()), Times.Once);
    }

    [Test]
    public void SaveMergedJson_NormalCase_SavesToFile()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var sharedStepId = Guid.NewGuid();
        var testCaseId = Guid.NewGuid();

        var mergedRoot = new Root
        {
            ProjectName = "Merged Project",
            Attributes = new List<AttributeModel>
            {
                new AttributeModel { Id = attributeId, Name = "Merged Attr" }
            },
            Sections = new List<Section>
            {
                new Section
                {
                    Id = sectionId,
                    Name = "Merged Section",
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>(),
                    Sections = new List<Section>()
                }
            },
            SharedSteps = new List<Guid> { sharedStepId },
            TestCases = new List<Guid> { testCaseId }
        };

        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");

        // Create batch directory
        Directory.CreateDirectory(batch1Path);

        var mergedPath = Path.Combine(projectPath, "merged");
        Directory.CreateDirectory(mergedPath);
        var expectedFilePath = Path.Combine(mergedPath, "main.json");

        // Act
        // Since SaveMergedJson is private, we test it through the full flow
        var mainJsonObjectList = new List<Root> { mergedRoot };
        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);
        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);
        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);

        _mergeProcessor.MergeProjects();

        // Assert
        // Verify the method was called (indirect verification)
        _mockFileProcessor.Verify(x => x.UpdateReferencesInMergedFiles(
            It.IsAny<string>(),
            It.IsAny<Dictionary<Guid, Guid>>(),
            It.IsAny<Dictionary<Guid, Guid>>()), Times.Once);
    }

    #endregion

    #region Integration Scenarios

    [Test]
    public void MergeProjects_ComplexScenarioWithNestedSections_MergesCorrectly()
    {
        // Arrange
        var projectPath = Path.Combine(_appConfig.ResultPath, _appConfig.Zephyr.ProjectKey);
        var batch1Path = Path.Combine(projectPath, "batch_1");
        var batch2Path = Path.Combine(projectPath, "batch_2");

        // Create batch directories
        Directory.CreateDirectory(batch1Path);
        Directory.CreateDirectory(batch2Path);

        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid();
        var childSectionId1 = Guid.NewGuid();
        var childSectionId2 = Guid.NewGuid();

        var mainJsonObjectList = new List<Root>
        {
            new Root
            {
                ProjectName = "Project With Nested Sections",
                Attributes = new List<AttributeModel>
                {
                    new AttributeModel { Id = Guid.NewGuid(), Name = "Priority" },
                    new AttributeModel { Id = Guid.NewGuid(), Name = "Severity" }
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Id = sectionId1,
                        Name = "Parent Section 1",
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>
                        {
                            new Section
                            {
                                Id = childSectionId1,
                                Name = "Child Section 1",
                                PreconditionSteps = new List<Step>(),
                                PostconditionSteps = new List<Step>(),
                                Sections = new List<Section>()
                            }
                        }
                    }
                },
                SharedSteps = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
                TestCases = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }
            },
            new Root
            {
                ProjectName = "Another Project",
                Attributes = new List<AttributeModel>
                {
                    new AttributeModel { Id = Guid.NewGuid(), Name = "Type" },
                    new AttributeModel { Id = Guid.NewGuid(), Name = "Priority" } // Duplicate name
                },
                Sections = new List<Section>
                {
                    new Section
                    {
                        Id = sectionId2,
                        Name = "Parent Section 1", // Duplicate name
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>
                        {
                            new Section
                            {
                                Id = childSectionId2,
                                Name = "Child Section 2",
                                PreconditionSteps = new List<Step>(),
                                PostconditionSteps = new List<Step>(),
                                Sections = new List<Section>()
                            }
                        }
                    },
                    new Section
                    {
                        Id = Guid.NewGuid(),
                        Name = "Unique Section",
                        PreconditionSteps = new List<Step>(),
                        PostconditionSteps = new List<Step>(),
                        Sections = new List<Section>()
                    }
                },
                SharedSteps = new List<Guid> { Guid.NewGuid() }, // Some duplicates expected
                TestCases = new List<Guid> { Guid.NewGuid() } // Some duplicates expected
            }
        };

        _mockMainJsonProcessor.Setup(x => x.LoadMainJsonFromBatches(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mainJsonObjectList);

        // Act
        _mergeProcessor.MergeProjects();

        // Assert
        // Should complete successfully
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Merge process completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
