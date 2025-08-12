using Models;

namespace ZephyrScaleServerExporter.Services;

public interface IWriteService
{
    Task<string> WriteAttachment(Guid testCaseId, byte[] content, string fileName, bool isSharedAttachment);
    Task WriteTestCase(global::Models.TestCase testCase);
    Task WriteSharedStep(SharedStep sharedStep);
    Task WriteMainJson(Root mainJson);
    Task<string?> CopyAttachment(Guid targetId, string fileName);

    int GetBatchNumber();
}
