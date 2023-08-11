using Importer.Models;

namespace Importer.Services;

public interface ITestCaseService
{
    Task ImportTestCases(IEnumerable<Guid> testCases, Dictionary<Guid, Guid> sections,
        Dictionary<Guid, TmsAttribute> attributes, Dictionary<Guid, Guid> sharedSteps);
}
