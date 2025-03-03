using Importer.Client;
using Microsoft.Extensions.Logging;

namespace Importer.Services.Implementations;

internal class ProjectService(ILogger<ProjectService> logger, IClientAdapter clientAdapter)
    : IProjectService
{
    public async Task<Guid> ImportProject(string projectName)
    {
        logger.LogInformation("Importing project");

        var projectId = await clientAdapter.GetProject(projectName);

        if (projectId != Guid.Empty) return projectId;

        return await clientAdapter.CreateProject(projectName);
    }
}