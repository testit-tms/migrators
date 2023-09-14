using Importer.Models;
using Models;
using Attribute = Models.Attribute;

namespace Importer.Client;

public interface IClient
{
    Task CreateProject(string name);
    Task<Guid> ImportSection(Guid parentSectionId, Section section);
    Task<TmsAttribute> ImportAttribute(Attribute attribute);
    Task<TmsAttribute> GetAttribute(Guid id);
    Task<Guid> ImportSharedStep(Guid parentSectionId, SharedStep sharedStep);
    Task ImportTestCase(Guid parentSectionId, TmsTestCase testCase);
    Task<Guid> GetRootSectionId();
    Task<List<TmsAttribute>> GetProjectAttributes();
    Task AddAttributesToProject(IEnumerable<Guid> attributeIds);
    Task<TmsAttribute> UpdateAttribute(TmsAttribute attribute);

    Task<Guid> UploadAttachment(string fileName, Stream content);
    Task<TmsParameter> CreateParameter(Parameter parameter);
    Task<List<TmsParameter>> GetParameter(string name);
}
