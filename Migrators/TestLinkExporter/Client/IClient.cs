using TestLinkExporter.Models.Attachment;
using TestLinkExporter.Models.Project;
using TestLinkExporter.Models.Suite;
using TestLinkExporter.Models.TestCase;

namespace TestLinkExporter.Client;

public interface IClient
{
    TestLinkProject GetProject();
    List<TestLinkSuite> GetSuitesByProjectId(int id);
    List<TestLinkSuite> GetSharedSuitesBySuiteId(int id);
    List<int> GetTestCaseIdsBySuiteId(int id);
    TestLinkTestCase GetTestCaseById(int id);
    List<string> GetKeywordsByTestCaseById(int id);
    List<TestLinkAttachment> GetAttachmentsByTestCaseId(int id);
}
