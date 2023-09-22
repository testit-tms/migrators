using AzureExporter.Client;
using AzureExporter.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Constants = AzureExporter.Models.Constants;

namespace AzureExporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger;
    private IClient _client;
    private IStepService _stepService;
    private IAttachmentService _attachmentService;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _client = Substitute.For<IClient>();
        _stepService = Substitute.For<IStepService>();
        _attachmentService = Substitute.For<IAttachmentService>();
    }

    [Test]
    public async Task ConvertTestCases_FailedGetWorkItems_ReturnsEmptyList()
    {
        // Arrange
        _client.GetWorkItems(Constants.TestCaseType)
            .ThrowsAsync(new Exception("Failed to get work items"));

        var testCaseService = new TestCaseService(_logger, _client, _stepService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(() =>
            testCaseService.ConvertTestCases(Guid.NewGuid(), new Dictionary<int, Guid>(),
                Guid.NewGuid(), new Dictionary<string, Guid>()));

        // Assert
        await _client.DidNotReceive()
            .GetWorkItemById(Arg.Any<int>());

        _stepService.DidNotReceive()
            .ConvertSteps(Arg.Any<string>(), Arg.Any<Dictionary<int, Guid>>());

        await _attachmentService.DidNotReceive()
            .DownloadAttachments(Arg.Any<List<WorkItemRelation>>(), Arg.Any<Guid>());
    }
}
