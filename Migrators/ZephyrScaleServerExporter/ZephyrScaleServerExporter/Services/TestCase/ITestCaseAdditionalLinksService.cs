using Models;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Services.TestCase;

public interface ITestCaseAdditionalLinksService
{
    Task<List<Link>> GetAdditionalLinks(ZephyrTestCase zephyrTestCase);

    Task<Link> ConvertIssueLinkByIssueId(string issueId);
}
