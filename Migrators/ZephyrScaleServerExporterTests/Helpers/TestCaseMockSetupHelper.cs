using Microsoft.Extensions.Options;
using Moq;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services.TestCase;

namespace ZephyrScaleServerExporterTests.Helpers;

public static class TestCaseMockSetupHelper
{
    public static void SetupGetTestCasesByConfig(
        this Mock<ITestCaseCommonService> mockService,
        Mock<IOptions<AppConfig>> mockAppConfig,
        Mock<IClient> mockClient,
        string stringStatuses,
        int startAt,
        int maxResults,
        List<ZephyrTestCase> returnValue)
    {
        mockService
            .Setup(s => s.GetTestCasesByConfig(
                mockAppConfig.Object,
                mockClient.Object,
                It.Is<int>(x => x == startAt),
                It.Is<int>(x => x == maxResults),
                It.Is<string>(x => x == stringStatuses)))
            .ReturnsAsync(returnValue);
    }

    public static void SetupGetArchivedTestCases(
        this Mock<ITestCaseCommonService> mockService,
        Mock<IOptions<AppConfig>> mockAppConfig,
        Mock<IClient> mockClient,
        string stringStatuses,
        int startAt,
        int maxResults,
        List<ZephyrTestCase> returnValue)
    {
        mockService
            .Setup(s => s.GetArchivedTestCases(
                mockAppConfig.Object,
                mockClient.Object,
                It.Is<int>(x => x == startAt),
                It.Is<int>(x => x == maxResults),
                It.Is<string>(x => x == stringStatuses)))
            .ReturnsAsync(returnValue);
    }

    public static void SetupGetTestCasesByConfigBatches(
        this Mock<ITestCaseCommonService> mockService,
        Mock<IOptions<AppConfig>> mockAppConfig,
        Mock<IClient> mockClient,
        string stringStatuses,
        Dictionary<(int startAt, int maxResults), List<ZephyrTestCase>> batches)
    {
        foreach (var batch in batches)
        {
            mockService.SetupGetTestCasesByConfig(
                mockAppConfig,
                mockClient,
                stringStatuses,
                batch.Key.startAt,
                batch.Key.maxResults,
                batch.Value);
        }
    }

    public static void SetupGetArchivedTestCasesBatches(
        this Mock<ITestCaseCommonService> mockService,
        Mock<IOptions<AppConfig>> mockAppConfig,
        Mock<IClient> mockClient,
        string stringStatuses,
        Dictionary<(int startAt, int maxResults), List<ZephyrTestCase>> batches)
    {
        foreach (var batch in batches)
        {
            mockService.SetupGetArchivedTestCases(
                mockAppConfig,
                mockClient,
                stringStatuses,
                batch.Key.startAt,
                batch.Key.maxResults,
                batch.Value);
        }
    }


    # region Verify

    public static void VerifyGetTestCasesByConfigBatches(
        this Mock<ITestCaseCommonService> mockService,
        Mock<IOptions<AppConfig>> mockAppConfig,
        Mock<IClient> mockClient,
        string stringStatuses,
        Dictionary<(int startAt, int maxResults), List<ZephyrTestCase>> batches)
    {
        foreach (var batch in batches)
        {
            mockService.Verify(s => s.GetTestCasesByConfig(
                mockAppConfig.Object,
                mockClient.Object,
                It.Is<int>(x => x == batch.Key.startAt),
                It.Is<int>(x => x == batch.Key.maxResults),
                It.Is<string>(x => x == stringStatuses)), Times.Once);
        }
    }

    public static void VerifyGetArchivedTestCasesBatches(
        this Mock<ITestCaseCommonService> mockService,
        Mock<IOptions<AppConfig>> mockAppConfig,
        Mock<IClient> mockClient,
        string stringStatuses,
        Dictionary<(int startAt, int maxResults), List<ZephyrTestCase>> batches)
    {
        foreach (var batch in batches)
        {
            mockService.Verify(s => s.GetArchivedTestCases(
                mockAppConfig.Object,
                mockClient.Object,
                It.Is<int>(x => x == batch.Key.startAt),
                It.Is<int>(x => x == batch.Key.maxResults),
                It.Is<string>(x => x == stringStatuses)), Times.Once);
        }
    }

    public static void VerifyGetTestCasesByConfigBatchesDirect(
        this Mock<ITestCaseCommonService> mockService,
        Mock<IOptions<AppConfig>> mockAppConfig,
        Mock<IClient> mockClient,
        string stringStatuses,
        Dictionary<(int startAt, int maxResults), List<ZephyrTestCase>> batches)
    {
        foreach (var batch in batches)
        {
            mockService.Verify(s => s.GetTestCasesByConfig(
                mockAppConfig.Object,
                mockClient.Object,
                batch.Key.startAt,
                batch.Key.maxResults,
                stringStatuses), Times.Once);
        }
    }

    public static void VerifyGetArchivedTestCasesBatchesDirect(
        this Mock<ITestCaseCommonService> mockService,
        Mock<IOptions<AppConfig>> mockAppConfig,
        Mock<IClient> mockClient,
        string stringStatuses,
        Dictionary<(int startAt, int maxResults), List<ZephyrTestCase>> batches)
    {
        foreach (var batch in batches)
        {
            mockService.Verify(s => s.GetArchivedTestCases(
                mockAppConfig.Object,
                mockClient.Object,
                batch.Key.startAt,
                batch.Key.maxResults,
                stringStatuses), Times.Once);
        }
    }
    #endregion
}
