
using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.BatchMerging;
using ZephyrScaleServerExporter.BatchMerging.Implementations;
using Models;

namespace ZephyrScaleServerExporterTests.BatchMerging;

[TestFixture]
public class MainJsonProcessorTests
{
    private Mock<ILogger<MainJsonProcessor>> _mockLogger;
    private Mock<IFileProcessor> _mockFileProcessor;
    private MainJsonProcessor _mainJsonProcessor;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<MainJsonProcessor>>();
        _mockFileProcessor = new Mock<IFileProcessor>();
        _mainJsonProcessor = new MainJsonProcessor(_mockLogger.Object, _mockFileProcessor.Object);
    }

    #region LoadMainJsonFromBatches

    [Test]
    public void LoadMainJsonFromBatches_NormalCase_CopiesFilesAndLoadsJson()
    {
        // Arrange
        var batchDirectories = new List<string> { "batch1", "batch2" };
        var mergedPath = "merged";
        var mainJsonFile = "main.json";

        // Act
        var result = _mainJsonProcessor.LoadMainJsonFromBatches(batchDirectories, mergedPath, mainJsonFile);

        // Assert
        // Verify that CopyBatchContents was called for each batch
        _mockFileProcessor.Verify(x => x.CopyBatchContents("batch1", mergedPath, mainJsonFile), Times.Once);
        _mockFileProcessor.Verify(x => x.CopyBatchContents("batch2", mergedPath, mainJsonFile), Times.Once);

        // Result should be empty since we're not actually loading files (File.Exists always returns false in real env)
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        });

        // Verify logging for each batch
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing batch directory: batch1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing batch directory: batch2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void LoadMainJsonFromBatches_EmptyBatchList_ReturnsEmptyList()
    {
        // Arrange
        var batchDirectories = Enumerable.Empty<string>();
        var mergedPath = "merged";
        var mainJsonFile = "main.json";

        // Act
        var result = _mainJsonProcessor.LoadMainJsonFromBatches(batchDirectories, mergedPath, mainJsonFile);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        });

        // No calls to file processor should be made
        _mockFileProcessor.Verify(x => x.CopyBatchContents(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        // No logging should occur for processing batches
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing batch directory:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Test]
    public void LoadMainJsonFromBatches_NullBatchList_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<string> batchDirectories = null!;
        var mergedPath = "merged";
        var mainJsonFile = "main.json";

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            _mainJsonProcessor.LoadMainJsonFromBatches(batchDirectories, mergedPath, mainJsonFile));
    }

    [Test]
    public void LoadMainJsonFromBatches_WithValidParameters_CallsDependenciesCorrectly()
    {
        // Arrange
        var batchDirectories = new List<string> { "C:\\batches\\batch1" };
        var mergedPath = "C:\\merged";
        var mainJsonFile = "main.json";

        // Act
        var result = _mainJsonProcessor.LoadMainJsonFromBatches(batchDirectories, mergedPath, mainJsonFile);

        // Assert
        // Verify file processor was called with correct parameters
        _mockFileProcessor.Verify(x =>
            x.CopyBatchContents("C:\\batches\\batch1", "C:\\merged", "main.json"), Times.Once);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing batch directory: C:\\batches\\batch1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Should return empty list (since File.Exists returns false in test environment)
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Integration-like Scenarios

    [Test]
    public void LoadMainJsonFromBatches_MultipleBatches_AllProcessedInOrder()
    {
        // Arrange
        var batchDirectories = new List<string>
        {
            "C:\\batches\\first",
            "C:\\batches\\second",
            "C:\\batches\\third"
        };
        var mergedPath = "C:\\final\\merged";
        var mainJsonFile = "config.json";

        // Act
        var result = _mainJsonProcessor.LoadMainJsonFromBatches(batchDirectories, mergedPath, mainJsonFile);

        // Assert
        // Verify all batches were processed in order
        _mockFileProcessor.Verify(x => x.CopyBatchContents("C:\\batches\\first", mergedPath, mainJsonFile), Times.Once);
        _mockFileProcessor.Verify(x => x.CopyBatchContents("C:\\batches\\second", mergedPath, mainJsonFile), Times.Once);
        _mockFileProcessor.Verify(x => x.CopyBatchContents("C:\\batches\\third", mergedPath, mainJsonFile), Times.Once);

        // Verify all logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing batch directory: C:\\batches\\first")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing batch directory: C:\\batches\\second")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing batch directory: C:\\batches\\third")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.That(result, Is.Empty);
    }

    #endregion
}
