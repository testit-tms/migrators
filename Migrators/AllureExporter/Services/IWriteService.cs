using Models;

namespace AllureExporter.Services;

public interface IWriteService
{
    Task WriteAttachment(Guid id, byte[] content, string fileName);
    Task WriteTestCase(TestCase testCase);
}
