using Importer.Models;
using Models;
using Attribute = Models.Attribute;

namespace Importer.Client;

public interface IClientAdapter
{
    Task<Guid> GetProject(string name);
    Task<Guid> CreateProject(string name);
    Task<Guid> ImportSection(Guid projectId, Guid parentSectionId, Section section);
    Task<TmsAttribute> ImportAttribute(Attribute attribute);
    Task<TmsAttribute> GetAttribute(Guid id);
    Task<Guid> ImportSharedStep(Guid projectId, Guid parentSectionId, SharedStep sharedStep);
    Task<bool> ImportTestCase(Guid projectId, Guid parentSectionId, TmsTestCase testCase);
    Task<Guid> GetRootSectionId(Guid projectId);
    Task<List<TmsAttribute>> GetProjectAttributes();
    Task<List<TmsAttribute>> GetRequiredProjectAttributesByProjectId(Guid projectId);
    Task<TmsAttribute> GetProjectAttributeById(Guid id);
    Task AddAttributesToProject(Guid projectId, IEnumerable<Guid> attributeIds);
    Task<TmsAttribute> UpdateAttribute(TmsAttribute attribute);
    Task UpdateProjectAttribute(Guid projectId, TmsAttribute attribute);
    Task<Guid> UploadAttachment(string fileName, Stream content);
    Task<TmsParameter> CreateParameter(Parameter parameter);
    Task<List<TmsParameter>> GetParameter(string name);
}