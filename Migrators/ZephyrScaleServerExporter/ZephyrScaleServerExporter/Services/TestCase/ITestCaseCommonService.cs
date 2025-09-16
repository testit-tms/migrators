using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Models.TestCases.Export;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Services.TestCase;

public interface ITestCaseCommonService
{
    Task<List<Guid>> WriteTestCasesAsync(
        List<ZephyrTestCase> cases,
        SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames,
        Attribute ownersAttribute);

    Task<List<Guid>> WriteTestCasesCloudAsync(
        List<CloudZephyrTestCase> cases,
        SectionData sectionData,
        Dictionary<string, Attribute> attributeMap,
        List<string> requiredAttributeNames,
        Attribute ownersAttribute);

    Task<TestCaseExportRequiredModel> PrepareForTestCasesExportAsync(
        Dictionary<string, Attribute> attributeMap,
        string projectId);

    Task<TestCaseExportRequiredModel> CloudPrepareForTestCasesExportAsync(
        Dictionary<string, Attribute> attributeMap,
        string projectId, string projectKey);

    TestCaseData PrepareTestCaseIdsData(
        Dictionary<string, Attribute> attributeMap,
        Attribute ownersAttribute,
        List<Guid> testCaseIds);

    Task<List<ZephyrTestCase>> GetTestCasesByConfig(IOptions<AppConfig> config,
        IClient client,
        int startAt, int maxResults, string statuses);

    Task<List<CloudZephyrTestCase>> GetTestCasesByConfigCloud(IOptions<AppConfig> config,
        IClient client,
        int startAt, int maxResults, string statuses);

    Task<List<ZephyrTestCase>> GetArchivedTestCases(IOptions<AppConfig> config,
        IClient client,
        int startAt, int maxResults, string statuses);
}
