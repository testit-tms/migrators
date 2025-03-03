namespace Importer.Services;

public interface IProjectService
{
    Task<Guid> ImportProject(string projectName);
}