using Importer.Models;
using Models;
using Attribute = Models.Attribute;

namespace Importer.Client;

public interface IClient
{
    Task CreateProject(string name);
    Task<Guid> ImportSection(Guid parentSectionId, Section section);
    Task<TmsAttribute> ImportAttribute(Attribute attribute);
    Task<Guid> ImportSharedStep(Guid parentSectionId, SharedStep sharedStep);
    Task ImportTestCase(Guid parentSectionId, TmsTestCase testCase);
    Task<Guid> GetRootSectionId();
    Task<List<TmsAttribute>> GetProjectAttributes();
    Task AddAttributesToProject(IEnumerable<Guid> attributeIds);

    Task<Guid> UploadAttachment(string fileName, Stream content);
    Task<TmsParameter> CreateParameter(Parameter parameter);
    Task<List<TmsParameter>> GetParameter(string name);
}
