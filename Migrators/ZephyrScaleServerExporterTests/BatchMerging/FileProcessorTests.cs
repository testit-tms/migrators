using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter;
using ZephyrScaleServerExporter.BatchMerging;
using ZephyrScaleServerExporter.BatchMerging.Implementations;

namespace ZephyrScaleServerExporterTests.BatchMerging;

[TestFixture]
public class FileProcessorTests
{
    private Mock<ILogger<App>> _mockLogger;
    private FileProcessor _fileProcessor;
    private string _tempDirectory;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<App>>();
        _fileProcessor = new FileProcessor(_mockLogger.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
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

    #region UpdateReferencesInMergedFiles

    [Test]
    public void UpdateReferencesInMergedFiles_WithEmptyMaps_LogsSkippingMessage()
    {
        // Arrange
        var mergedPath = "test/path";
        var sectionMap = new Dictionary<Guid, Guid>();
        var attributeMap = new Dictionary<Guid, Guid>();

        // Act
        _fileProcessor.UpdateReferencesInMergedFiles(mergedPath, sectionMap, attributeMap);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No duplicate sections or attributes found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void UpdateReferencesInMergedFiles_WithSectionAndAttributeMaps_UpdatesReferencesInJsonFiles()
    {
        // Arrange
        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid();
        var attributeId1 = Guid.NewGuid();
        var attributeId2 = Guid.NewGuid();

        var sectionMap = new Dictionary<Guid, Guid>
        {
            { sectionId1, sectionId2 }
        };

        var attributeMap = new Dictionary<Guid, Guid>
        {
            { attributeId1, attributeId2 }
        };

        // Create test JSON file with references to replace
        var testFile = Path.Combine(_tempDirectory, "test.json");
        var originalContent = $@"{{
          ""sectionReference"": ""{sectionId1}"",
          ""attributes"": [
            {{
              ""id"": ""{attributeId1}"",
              ""name"": ""Test Attribute""
            }}
          ]
        }}";
        File.WriteAllText(testFile, originalContent);

        // Act
        _fileProcessor.UpdateReferencesInMergedFiles(_tempDirectory, sectionMap, attributeMap);

        // Assert
        var updatedContent = File.ReadAllText(testFile);
        Assert.That(updatedContent, Does.Contain(sectionId2.ToString()));
        Assert.That(updatedContent, Does.Contain(attributeId2.ToString()));
        Assert.That(updatedContent, Does.Not.Contain(sectionId1.ToString()));
        Assert.That(updatedContent, Does.Not.Contain(attributeId1.ToString()));

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting text-based update")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("files updated with a total of")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void UpdateReferencesInMergedFiles_WithNoMatches_DoesNotModifyFiles()
    {
        // Arrange
        var sectionMap = new Dictionary<Guid, Guid>
        {
            { Guid.NewGuid(), Guid.NewGuid() }
        };

        var attributeMap = new Dictionary<Guid, Guid>
        {
            { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Create test JSON file without matching references
        var testFile = Path.Combine(_tempDirectory, "test.json");
        var originalContent = @"{
          ""sectionReference"": ""unrelated-content"",
          ""attributes"": [
            {
              ""id"": ""unrelated-id"",
              ""name"": ""Test Attribute""
            }
          ]
        }";
        File.WriteAllText(testFile, originalContent);

        // Act
        _fileProcessor.UpdateReferencesInMergedFiles(_tempDirectory, sectionMap, attributeMap);

        // Assert
        var updatedContent = File.ReadAllText(testFile);
        Assert.That(updatedContent, Is.EqualTo(originalContent));

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("files updated with a total of 0 replacements")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void UpdateReferencesInMergedFiles_WithMultipleJsonFiles_ProcessesAll()
    {
        // Arrange
        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid();

        var sectionMap = new Dictionary<Guid, Guid>
        {
            { sectionId1, sectionId2 }
        };

        var attributeMap = new Dictionary<Guid, Guid>();

        // Create multiple JSON files
        var testFile1 = Path.Combine(_tempDirectory, "test1.json");
        var testFile2 = Path.Combine(_tempDirectory, "test2.json");
        var testFile3 = Path.Combine(_tempDirectory, "test3.txt"); // Not JSON, should be skipped

        File.WriteAllText(testFile1, $"{{\"sectionReference\": \"{sectionId1}\"}}");
        File.WriteAllText(testFile2, $"{{\"anotherReference\": \"{sectionId1}\"}}");
        File.WriteAllText(testFile3, $"{{\"sectionReference\": \"{sectionId1}\"}}"); // Should not be processed

        // Act
        _fileProcessor.UpdateReferencesInMergedFiles(_tempDirectory, sectionMap, attributeMap);

        // Assert
        Assert.Multiple (()=> {
            // JSON files should be updated
            Assert.That(File.ReadAllText(testFile1), Does.Contain(sectionId2.ToString()));
            Assert.That(File.ReadAllText(testFile2), Does.Contain(sectionId2.ToString()));

            // Non-JSON file should remain unchanged
            Assert.That(File.ReadAllText(testFile3), Does.Contain(sectionId1.ToString()));
            Assert.That(File.ReadAllText(testFile3), Does.Not.Contain(sectionId2.ToString()));
        });

    }

    #endregion

    #region CopyBatchContents

    [Test]
    public void CopyBatchContents_NormalCase_CopiesFilesAndDirectories()
    {
        // Arrange
        var batchSourcePath = Path.Combine(_tempDirectory, "source");
        var mergedPath = Path.Combine(_tempDirectory, "merged");
        var fileToExclude = "exclude.txt";

        Directory.CreateDirectory(batchSourcePath);
        Directory.CreateDirectory(mergedPath);

        // Create test files in source
        var file1 = Path.Combine(batchSourcePath, "file1.txt");
        var file2 = Path.Combine(batchSourcePath, "file2.txt");
        var excludeFile = Path.Combine(batchSourcePath, fileToExclude);
        var subDir = Path.Combine(batchSourcePath, "subdir");
        var subFile = Path.Combine(subDir, "subfile.txt");

        Directory.CreateDirectory(subDir);
        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");
        File.WriteAllText(excludeFile, "exclude content");
        File.WriteAllText(subFile, "sub content");

        // Act
        _fileProcessor.CopyBatchContents(batchSourcePath, mergedPath, fileToExclude);

        // Assert
        Assert.Multiple (()=> {
            // Files should be copied (except excluded one)
            Assert.That(File.Exists(Path.Combine(mergedPath, "file1.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(mergedPath, "file2.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(mergedPath, fileToExclude)), Is.False);
            Assert.That(File.Exists(Path.Combine(mergedPath, "subdir", "subfile.txt")), Is.True);

            // Content should match
            Assert.That(File.ReadAllText(Path.Combine(mergedPath, "file1.txt")), Is.EqualTo("content1"));
            Assert.That(File.ReadAllText(Path.Combine(mergedPath, "file2.txt")), Is.EqualTo("content2"));
            Assert.That(File.ReadAllText(Path.Combine(mergedPath, "subdir", "subfile.txt")), Is.EqualTo("sub content"));
        });

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Copied file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Copied subdirectory")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void CopyBatchContents_WithFileToExclude_ExcludesSpecifiedFile()
    {
        // Arrange
        var batchSourcePath = Path.Combine(_tempDirectory, "source");
        var mergedPath = Path.Combine(_tempDirectory, "merged");
        var fileToExclude = "main.json";

        Directory.CreateDirectory(batchSourcePath);
        Directory.CreateDirectory(mergedPath);

        // Create files including the one to exclude
        var mainJsonFile = Path.Combine(batchSourcePath, fileToExclude);
        var otherFile = Path.Combine(batchSourcePath, "other.txt");

        File.WriteAllText(mainJsonFile, "{\"project\": \"test\"}");
        File.WriteAllText(otherFile, "other content");

        // Act
        _fileProcessor.CopyBatchContents(batchSourcePath, mergedPath, fileToExclude);

        // Assert
        Assert.Multiple (()=> {
            // Excluded file should not be copied
            Assert.That(File.Exists(Path.Combine(mergedPath, fileToExclude)), Is.False);
            // Other files should be copied
            Assert.That(File.Exists(Path.Combine(mergedPath, "other.txt")), Is.True);
        });
    }

    [Test]
    public void CopyBatchContents_WithFileCopyException_LogsWarningAndContinues()
    {
        // Arrange
        var batchSourcePath = Path.Combine(_tempDirectory, "source");
        var mergedPath = Path.Combine(_tempDirectory, "merged");
        var fileToExclude = "exclude.txt";

        Directory.CreateDirectory(batchSourcePath);
        Directory.CreateDirectory(mergedPath);

        // Create files
        var readableFile = Path.Combine(batchSourcePath, "readable.txt");
        var copyableFile = Path.Combine(batchSourcePath, "copyable.txt");

        File.WriteAllText(readableFile, "readable content");
        File.WriteAllText(copyableFile, "copyable content");

        // Act
        _fileProcessor.CopyBatchContents(batchSourcePath, mergedPath, fileToExclude);

        // Assert
        // Both files should be copied successfully in normal conditions
        Assert.Multiple (()=> {
            Assert.That(File.Exists(Path.Combine(mergedPath, "readable.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(mergedPath, "copyable.txt")), Is.True);
        });

        // Verify logging for successful copies
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Copied file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2));
    }

    #endregion
}
