using Importer.Client;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services;

public class ProjectService : IProjectService
{
    private readonly ILogger<ProjectService> _logger;
    private readonly IClient _client;

    public ProjectService(ILogger<ProjectService> logger, IClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<Guid> ImportProject(string projectName)
    {
        _logger.LogInformation("Importing project");

        var projectId = await _client.GetProject(projectName);

        if (projectId != Guid.Empty)
        {
            return projectId;
        }

        return await _client.CreateProject(projectName);
    }
}
