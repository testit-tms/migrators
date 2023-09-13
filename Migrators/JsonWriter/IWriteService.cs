using Models;

namespace JsonWriter;

public interface IWriteService
{
    Task<string> WriteAttachment(Guid id, byte[] content, string fileName);
    Task WriteTestCase(TestCase testCase);
    Task WriteMainJson(Root mainJson);
}
