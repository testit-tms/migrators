using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Implementations;
using ZephyrScaleServerExporterTests.Helpers;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class StatusServiceTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<ILogger<StatusService>> _mockLogger;
    private Mock<IOptions<AppConfig>> _mockAppConfig;
    private Mock<IClient> _mockClient;
    private StatusService _statusService;
    private string _projectId;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockLogger = new Mock<ILogger<StatusService>>();
        _mockAppConfig = new Mock<IOptions<AppConfig>>();
        _mockClient = new Mock<IClient>();

        var appConfig = new AppConfig
        {
            Zephyr = new ZephyrConfig
            {
                IgnoreFilter = false
            }
        };
        _mockAppConfig.Setup(x => x.Value).Returns(appConfig);

        _statusService = new StatusService(
            _mockDetailedLogService.Object,
            _mockLogger.Object,
            _mockAppConfig.Object,
            _mockClient.Object);

        _projectId = TestDataHelper.GenerateProjectId().ToString();
    }

    #region ConvertStatuses

    [Test]
    public async Task ConvertStatuses_WithMultipleStatuses_ReturnsCorrectStatusData()
    {
        // Arrange
        var statuses = new List<ZephyrStatus>
        {
            new() { Id = 1, Name = "Passed" },
            new() { Id = 2, Name = "Failed" },
            new() { Id = 3, Name = "Blocked" },
            new() { Id = 4, Name = "In Progress" }
        };

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ReturnsAsync(statuses);

        // Act
        var result = await _statusService.ConvertStatuses(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertStatusData(result, statuses);

            AssertStatusAttribute(
                result!.StatusAttribute,
                expectedName: Constants.ZephyrStatusAttribute,
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 4,
                "Passed", "Failed", "Blocked", "In Progress");

            Assert.That(result.StringStatuses, Is.EqualTo("\"Passed\",\"Failed\",\"Blocked\",\"In Progress\""));
            Assert.That(result.StringStatuses, Does.Not.EndWith(","));
            Assert.That(result.StringStatuses.Split(','), Has.Length.EqualTo(4));

            Assert.That(result.StatusAttribute.Options[0], Is.EqualTo("Passed"));
            Assert.That(result.StatusAttribute.Options[1], Is.EqualTo("Failed"));
            Assert.That(result.StatusAttribute.Options[2], Is.EqualTo("Blocked"));
            Assert.That(result.StatusAttribute.Options[3], Is.EqualTo("In Progress"));
        });

        _mockClient.Verify(x => x.GetStatuses(_projectId), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting statuses")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockDetailedLogService.Verify(
            x => x.LogInformation("Converted statuses \"{StringStatuses}\"", It.Is<object[]>(o => o[0]!.ToString() == "\"Passed\",\"Failed\",\"Blocked\",\"In Progress\"")),
            Times.Once);
    }

    [Test]
    public async Task ConvertStatuses_WithSingleStatus_ReturnsCorrectStatusData()
    {
        // Arrange
        var statuses = new List<ZephyrStatus>
        {
            new() { Id = 1, Name = "Passed" }
        };

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ReturnsAsync(statuses);

        // Act
        var result = await _statusService.ConvertStatuses(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertStatusData(result, statuses);

            AssertStatusAttribute(
                result!.StatusAttribute,
                expectedName: Constants.ZephyrStatusAttribute,
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 1,
                "Passed");
        });

        _mockClient.Verify(x => x.GetStatuses(_projectId), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting statuses")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockDetailedLogService.Verify(
            x => x.LogInformation(
                "Converted statuses \"{StringStatuses}\"",
                It.Is<object[]>(o => o[0]!.ToString() == result.StringStatuses)),
            Times.Once);
    }

    [Test]
    public async Task ConvertStatuses_WithEmptyStatusesList_ReturnsEmptyStringStatuses()
    {
        // Arrange
        var statuses = new List<ZephyrStatus>();

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ReturnsAsync(statuses);

        // Act
        var result = await _statusService.ConvertStatuses(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertStatusData(result, statuses);

            AssertStatusAttribute(
                result!.StatusAttribute,
                expectedName: Constants.ZephyrStatusAttribute,
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 0);
        });

        _mockClient.Verify(x => x.GetStatuses(_projectId), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting statuses")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockDetailedLogService.Verify(
            x => x.LogInformation(
                "Converted statuses \"{StringStatuses}\"",
                It.Is<object[]>(o => o[0]!.ToString() == string.Empty)),
            Times.Once);
    }

    [Test]
    public async Task ConvertStatuses_WithSpecialCharactersInStatusNames_HandlesCorrectly()
    {
        // Arrange
        var statuses = new List<ZephyrStatus>
        {
            new() { Id = 1, Name = "Status/With-Special_Chars" },
            new() { Id = 2, Name = "Status@With#Special$Chars%" },
            new() { Id = 3, Name = "Status With Spaces" },
            new() { Id = 4, Name = "Status\"With\"Quotes" }
        };

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ReturnsAsync(statuses);

        // Act
        var result = await _statusService.ConvertStatuses(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertStatusData(result, statuses);
            
            AssertStatusAttribute(
                result!.StatusAttribute,
                expectedName: Constants.ZephyrStatusAttribute,
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 4,
                "Status/With-Special_Chars", "Status@With#Special$Chars%", "Status With Spaces", "Status\"With\"Quotes");
        });
    }

    [Test]
    public async Task ConvertStatuses_WithDuplicateStatusNames_IncludesAllInStringStatuses()
    {
        // Arrange
        var statuses = new List<ZephyrStatus>
        {
            new() { Id = 1, Name = "Passed" },
            new() { Id = 2, Name = "Passed" },
            new() { Id = 3, Name = "Failed" }
        };

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ReturnsAsync(statuses);

        // Act
        var result = await _statusService.ConvertStatuses(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertStatusData(result, statuses);

            Assert.That(result!.StatusAttribute.Options, Has.Count.EqualTo(3));
            Assert.That(result.StatusAttribute.Options, Contains.Item("Passed"));
            Assert.That(result.StatusAttribute.Options, Contains.Item("Failed"));
        });
    }

    [Test]
    public async Task ConvertStatuses_StatusAttributeHasUniqueId()
    {
        // Arrange
        var statuses = new List<ZephyrStatus>
        {
            new() { Id = 1, Name = "Passed" },
            new() { Id = 2, Name = "Failed" }
        };

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ReturnsAsync(statuses);

        // Act
        var result1 = await _statusService.ConvertStatuses(_projectId);
        var result2 = await _statusService.ConvertStatuses(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result1.StatusAttribute.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result2.StatusAttribute.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result1.StatusAttribute.Id, Is.Not.EqualTo(result2.StatusAttribute.Id));
        });
    }

    [Test]
    public async Task ConvertStatuses_WithIgnoreFilterTrue_IncludesAllStatuses()
    {
        // Arrange
        var appConfig = new AppConfig
        {
            Zephyr = new ZephyrConfig
            {
                IgnoreFilter = true
            }
        };
        _mockAppConfig.Setup(x => x.Value).Returns(appConfig);

        var statusService = new StatusService(
            _mockDetailedLogService.Object,
            _mockLogger.Object,
            _mockAppConfig.Object,
            _mockClient.Object);

        var statuses = new List<ZephyrStatus>
        {
            new() { Id = 1, Name = "Passed" },
            new() { Id = 2, Name = "Failed" },
            new() { Id = 3, Name = "Blocked" }
        };

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ReturnsAsync(statuses);

        // Act
        var result = await statusService.ConvertStatuses(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertStatusData(result, statuses);
            Assert.That(result!.StatusAttribute.Options, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task ConvertStatuses_WithEmptyProjectId_HandlesGracefully()
    {
        // Arrange
        var projectId = string.Empty;
        var statuses = new List<ZephyrStatus>
        {
            new() { Id = 1, Name = "Passed" }
        };

        _mockClient.Setup(x => x.GetStatuses(projectId)).ReturnsAsync(statuses);

        // Act
        var result = await _statusService.ConvertStatuses(projectId);

        // Assert
        AssertStatusData(result, statuses);
    }

    [Test]
    public async Task ConvertStatuses_WithVeryLongStatusName_HandlesCorrectly()
    {
        // Arrange
        var longStatusName = new string('A', 1000);
        var statuses = new List<ZephyrStatus>
        {
            new() { Id = 1, Name = longStatusName }
        };

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ReturnsAsync(statuses);

        // Act
        var result = await _statusService.ConvertStatuses(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertStatusData(result, statuses);
            Assert.That(result!.StatusAttribute.Options, Contains.Item(longStatusName));
        });
    }

    [TestCase("Exception", "Failed to get statuses")]
    [TestCase("HttpRequestException", "Network error")]
    [TestCase("TaskCanceledException", "Request timeout")]
    public async Task ConvertStatuses_ClientThrowsException_PropagatesException(string exceptionTypeName, string errorMessage)
    {
        // Arrange
        Exception exception = exceptionTypeName switch
        {
            "Exception" => new Exception(errorMessage),
            "HttpRequestException" => new HttpRequestException(errorMessage),
            "TaskCanceledException" => new TaskCanceledException(errorMessage),
            _ => throw new ArgumentException($"Unknown exception type: {exceptionTypeName}")
        };

        _mockClient.Setup(x => x.GetStatuses(_projectId)).ThrowsAsync(exception);

        // Act & Assert
        Assert.That(
            async () => await _statusService.ConvertStatuses(_projectId),
            Throws.Exception.EqualTo(exception));
    }

    #endregion

    #region Assert Helpers

    private static void AssertStatusData(StatusData? result, List<ZephyrStatus> statuses)
    {
        var expectedStringStatuses = string.Join(",", statuses.Select(s => $"\"{s.Name}\""));
        
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.StringStatuses, Is.EqualTo(expectedStringStatuses));
            Assert.That(result.StatusAttribute, Is.Not.Null);
        });
    }

    private static void AssertStatusAttribute(
        Attribute? statusAttribute,
        string expectedName,
        AttributeType expectedType,
        bool expectedIsRequired,
        bool expectedIsActive,
        int expectedOptionsCount = 0,
        params string[] expectedOptions)
    {
        Assert.Multiple(() =>
        {
            Assert.That(statusAttribute, Is.Not.Null);
            Assert.That(statusAttribute!.Name, Is.EqualTo(expectedName));
            Assert.That(statusAttribute.Type, Is.EqualTo(expectedType));
            Assert.That(statusAttribute.IsRequired, Is.EqualTo(expectedIsRequired));
            Assert.That(statusAttribute.IsActive, Is.EqualTo(expectedIsActive));
            Assert.That(statusAttribute.Options, Is.Not.Null);
            Assert.That(statusAttribute.Options, Has.Count.EqualTo(expectedOptionsCount));
        });

        foreach (var option in expectedOptions)
        {
            Assert.That(statusAttribute.Options, Contains.Item(option));
        }
    }

    #endregion
}
