using PractiTestExporter.Models;

namespace PractiTestExporter.Client;

public interface IClient
{
    Task<PractiTestProject> GetProject();
    Task<List<PractiTestTestCase>> GetTestCases();
    Task<PractiTestTestCase> GetTestCaseById(string id);
    Task<List<PractiTestStep>> GetStepsByTestCaseId(string testCaseId);
    Task<List<PractiTestAttachment>> GetAttachmentsByEntityId(string entityType, string entityId);
    Task<byte[]> DownloadAttachmentById(string id);
    Task<List<PractiTestCustomField>> GetCustomFields();
    Task<ListPractiTestCustomField> GetListCustomFieldById(string id);
}
