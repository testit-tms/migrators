using Models;

namespace Importer.Services;

public interface IParserService
{
    Task<Root> GetMainFile();
    Task<SharedStep> GetSharedStep(Guid guid);
    Task<TestCase> GetTestCase(Guid guid);
    Task<FileStream> GetAttachment(Guid guid, string fileName);
}