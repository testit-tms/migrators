using SpiraTestExporter.Models;

namespace SpiraTestExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> GetAttachments(Guid testCaseId, int projectId, ArtifactType artifactType, int artifactId);
}
