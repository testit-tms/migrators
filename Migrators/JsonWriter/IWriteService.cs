using Models;

namespace JsonWriter;

public interface IWriteService
{
    Task WriteAttachment(Guid id, byte[] content, string fileName);
    Task WriteTestCase(TestCase testCase);
    Task WriteMainJson(Root mainJson);
}
