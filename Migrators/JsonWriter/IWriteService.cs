using Models;

namespace JsonWriter;

public interface IWriteService
{
    Task<string> WriteAttachment(Guid id, byte[] content, string fileName);
    Task WriteTestCase(TestCase testCase);
    Task WriteSharedStep(SharedStep sharedStep);
    Task WriteMainJson(Root mainJson);
}
