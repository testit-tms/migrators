using TestLinkExporter.Models;

namespace TestLinkExporter.Client;

public interface IClient
{
    TestLinkProject GetProject();
    List<TestLinkSuite> GetSuitesByProjectId(int id);
    List<TestLinkSuite> GetSharedSuitesBySuiteId(int id);
    List<int> GetTestCaseIdsBySuiteId(int id);
    TestLinkTestCase GetTestCaseById(int id);
    List<TestLinkAttachment> GetAttachmentsByTestCaseId(int id);
}
