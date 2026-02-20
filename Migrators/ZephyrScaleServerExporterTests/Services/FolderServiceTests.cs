using Models;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Implementations;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class FolderServiceTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private FolderService _folderService;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _folderService = new FolderService(_mockDetailedLogService.Object);
    }

    #region ConvertSections

    [TestCase("Test Project")]
    [TestCase("")]
    [TestCase("Project/With-Special_Chars@#%$")]
    [TestCase("Project With Multiple Spaces")]
    [TestCase("Проект с кириллицей")]
    public void ConvertSections_WithDifferentProjectNames_ReturnsCorrectSectionData(string projectName)
    {
        // Arrange & Act
        var result = _folderService.ConvertSections(projectName);

        // Assert
        Assert.Multiple(() =>
        {
            AssertSectionData(result, projectName);
            AssertSectionProperties(result, projectName);
            AssertSectionMappings(result, projectName);
        });

        VerifyLogging(projectName, result);
    }

    [Test]
    public void ConvertSections_WithNullProjectName_ReturnsCorrectSectionData()
    {
        // Arrange
        string? projectName = null;

        // Act
        var result = _folderService.ConvertSections(projectName!);

        // Assert
        Assert.Multiple(() =>
        {
            AssertSectionData(result, projectName!);
            AssertSectionProperties(result, projectName!);
            AssertSectionMappings(result, projectName!);
        });

        VerifyLogging(projectName!, result);
    }

    [Test]
    public void ConvertSections_WithVeryLongProjectName_HandlesCorrectly()
    {
        // Arrange
        var projectName = new string('A', 1000);

        // Act
        var result = _folderService.ConvertSections(projectName);

        // Assert
        Assert.Multiple(() =>
        {
            AssertSectionData(result, projectName);
            Assert.That(result.MainSection.Name, Is.EqualTo(projectName));
            Assert.That(result.MainSection.Name.Length, Is.EqualTo(1000));
            AssertSectionMappings(result, projectName);
        });

        VerifyLogging(projectName, result);
    }

    [TestCase("Project One", "Project Two")]
    [TestCase("Test Project", "Test Project")]
    public void ConvertSections_WithDifferentInstances_CreateUniqueSections(string projectName1, string projectName2)
    {
        // Arrange
        var folderService1 = new FolderService(_mockDetailedLogService.Object);
        var folderService2 = new FolderService(_mockDetailedLogService.Object);

        // Act
        var result1 = folderService1.ConvertSections(projectName1);
        var result2 = folderService2.ConvertSections(projectName2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result1.MainSection.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result2.MainSection.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result1.MainSection.Id, Is.Not.EqualTo(result2.MainSection.Id));
            Assert.That(result1.MainSection.Name, Is.EqualTo(projectName1));
            Assert.That(result2.MainSection.Name, Is.EqualTo(projectName2));
        });
    }

    [Test]
    public void ConvertSections_ReturnsCorrectSectionDataStructure()
    {
        // Arrange
        var projectName = "Test Project";

        // Act
        var result = _folderService.ConvertSections(projectName);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.SectionMap, Is.Not.Null);
            Assert.That(result.SectionMap.ContainsKey(Constants.MainFolderKey), Is.True);
            Assert.That(result.SectionMap[Constants.MainFolderKey], Is.EqualTo(result.MainSection.Id));
            Assert.That(result.SectionMap, Has.Count.EqualTo(1));

            Assert.That(result.AllSections, Is.Not.Null);
            Assert.That(result.AllSections.ContainsKey(Constants.MainFolderKey), Is.True);
            Assert.That(result.AllSections[Constants.MainFolderKey], Is.EqualTo(result.MainSection));
            Assert.That(result.AllSections, Has.Count.EqualTo(1));

            Assert.That(result.MainSection, Is.Not.Null);
            Assert.That(result.AllSections[Constants.MainFolderKey], Is.EqualTo(result.MainSection));
            Assert.That(result.AllSections[Constants.MainFolderKey].Id, Is.EqualTo(result.MainSection.Id));
            Assert.That(result.AllSections[Constants.MainFolderKey].Name, Is.EqualTo(result.MainSection.Name));

            Assert.That(result.SectionMap[Constants.MainFolderKey], Is.EqualTo(result.AllSections[Constants.MainFolderKey].Id));
            Assert.That(result.SectionMap.Keys, Is.EquivalentTo(result.AllSections.Keys));

            Assert.That(result.MainSection.Sections, Is.Not.Null);
            Assert.That(result.MainSection.Sections, Is.Empty);
            Assert.That(result.MainSection.PostconditionSteps, Is.Not.Null);
            Assert.That(result.MainSection.PostconditionSteps, Is.Empty);
            Assert.That(result.MainSection.PreconditionSteps, Is.Not.Null);
            Assert.That(result.MainSection.PreconditionSteps, Is.Empty);
        });
    }

    [Test]
    public void ConvertSections_CalledTwiceOnSameInstance_ThrowsArgumentException()
    {
        // Arrange
        var projectName = "Test Project";
        _folderService.ConvertSections(projectName);

        // Act & Assert
        Assert.That(
            () => _folderService.ConvertSections(projectName),
            Throws.ArgumentException.With.Message.Contain("has already been added"));
    }

    #endregion

    #region Assert Helpers

    private static void AssertSectionData(SectionData? result, string expectedProjectName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.MainSection, Is.Not.Null);
            Assert.That(result.SectionMap, Is.Not.Null);
            Assert.That(result.AllSections, Is.Not.Null);
        });
    }

    private static void AssertSectionProperties(SectionData? result, string expectedProjectName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.MainSection, Is.Not.Null);

            var section = result.MainSection;

            Assert.That(section.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(section.Name, Is.EqualTo(expectedProjectName));
            Assert.That(section.Sections, Is.Not.Null);
            Assert.That(section.Sections, Is.Empty);
            Assert.That(section.PostconditionSteps, Is.Not.Null);
            Assert.That(section.PostconditionSteps, Is.Empty);
            Assert.That(section.PreconditionSteps, Is.Not.Null);
            Assert.That(section.PreconditionSteps, Is.Empty);
        });
    }

    private static void AssertSectionMappings(SectionData? result, string expectedProjectName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result!.SectionMap.ContainsKey(Constants.MainFolderKey), Is.True);
            Assert.That(result.SectionMap[Constants.MainFolderKey], Is.EqualTo(result.MainSection.Id));
            Assert.That(result.AllSections.ContainsKey(Constants.MainFolderKey), Is.True);
            Assert.That(result.AllSections[Constants.MainFolderKey], Is.EqualTo(result.MainSection));
            Assert.That(result.SectionMap, Has.Count.EqualTo(1));
            Assert.That(result.AllSections, Has.Count.EqualTo(1));
        });
    }

    private void VerifyLogging(string projectName, SectionData result)
    {
        _mockDetailedLogService.Verify(
            x => x.LogDebug("Creating main section with name {@Name}", It.Is<object[]>(o => o[0] == null ? projectName == null : o[0]!.ToString() == projectName)),
            Times.Once);

        _mockDetailedLogService.Verify(
            x => x.LogDebug("Sections: {@SectionData}", It.Is<object[]>(o => o[0] is SectionData)),
            Times.Once);

        _mockDetailedLogService.Verify(
            x => x.LogDebug(It.IsAny<string>(), It.IsAny<object[]>()),
            Times.Exactly(2));
    }

    #endregion
}
