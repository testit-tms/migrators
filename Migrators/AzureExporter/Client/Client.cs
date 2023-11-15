using AzureExporter.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExporter.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;

    private readonly ProjectHttpClient _projectClient;
    private readonly WorkItemTrackingHttpClient _workItemTrackingClient;
    private readonly WorkHttpClient _workHttpClient;
    private readonly string _projectName;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("azure");
        var url = section["url"];

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        var token = section["token"];

        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Private token is not specified");
        }

        var projectName = section["projectName"];

        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("Project name is not specified");
        }

        _projectName = projectName;

        var connection = new VssConnection(new Uri(url), new VssBasicCredential(string.Empty, token));
        _projectClient = connection.GetClient<ProjectHttpClient>();
        _workItemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();
        _workHttpClient = connection.GetClient<WorkHttpClient>();
    }

    public async Task<AzureProject> GetProject()
    {
        var projects = _projectClient.GetProjects().Result;
        var project = projects.FirstOrDefault(p =>
            p.Name.Equals(_projectName, StringComparison.InvariantCultureIgnoreCase));

        if (project == null)
        {
            throw new ArgumentException($"Project {_projectName} is not found");
        }

        return new AzureProject
        {
            Id = project.Id,
            Name = project.Name
        };
    }

    public async Task<List<int>> GetWorkItemIds(string workItemType)
    {
        var wiql = new Wiql
        {
            Query = "SELECT [System.Id] " +
                    "FROM WorkItems " +
                    $"WHERE [System.TeamProject] = '{_projectName}' " +
                    $"AND [System.WorkItemType] = '{workItemType}'"
        };

        var queryResult = _workItemTrackingClient.QueryByWiqlAsync(wiql).Result;

        return queryResult.WorkItems.Select(w => w.Id).ToList();
    }

    public async Task<AzureWorkItem> GetWorkItemById(int id)
    {
        _logger.LogInformation("Getting work item with ID {Id}", id);

        var workItem = _workItemTrackingClient.GetWorkItemAsync(_projectName, id, expand: WorkItemExpand.All).Result;

        _logger.LogDebug("Work item: {@WorkItem}", workItem);

        var result = new AzureWorkItem
        {
            Id = workItem.Id!.Value,
            Title = GetValueOfField(workItem.Fields, "System.Title"),
            Description = GetValueOfField(workItem.Fields, "System.Description"),
            State = GetValueOfField(workItem.Fields, "System.State"),
            Priority = workItem.Fields["Microsoft.VSTS.Common.Priority"] as int? ?? 3,
            Steps = GetValueOfField(workItem.Fields, "Microsoft.VSTS.TCM.Steps"),
            IterationPath = GetValueOfField(workItem.Fields, "System.IterationPath"),
            Tags = GetValueOfField(workItem.Fields, "System.Tags"),
            Links = workItem.Relations == null
                ? new List<AzureLink>()
                : workItem.Relations
                    .Where(r => r.Rel == "ArtifactLink")
                    .Select(r => new AzureLink
                    {
                        Title = GetValueOfField(r.Attributes, "name"),
                        Url = r.Url
                    })
                    .ToList(),
            Attachments = workItem.Relations == null
                ? new List<AzureAttachment>()
                : workItem.Relations
                    .Where(r => r.Rel == "AttachedFile")
                    .Select(r => new AzureAttachment
                    {
                        Id = new Guid(r.Url[^36..]),
                        Name = GetValueOfField(r.Attributes, "name"),
                    })
                    .ToList(),
            Parameters = new AzureParameters
            {
                Keys = GetValueOfField(workItem.Fields, "Microsoft.VSTS.TCM.Parameters"),
                Values = GetValueOfField(workItem.Fields, "Microsoft.VSTS.TCM.LocalDataSource")
            }
        };

        return result;
    }

    public async Task<List<string>> GetIterations(Guid projectId)
    {
        _logger.LogInformation("Getting iterations");

        var iterations = _workHttpClient.GetTeamIterationsAsync(new TeamContext(projectId)).Result;
        var paths = iterations
            .Select(i => i.Path)
            .ToList();

        _logger.LogDebug("Got iterations: {@Iterations}", paths);

        return paths;
    }

    public async Task<byte[]> GetAttachmentById(Guid id)
    {
        var attachStream = _workItemTrackingClient.GetAttachmentContentAsync(id).Result;

        return UseStreamDotReadMethod(attachStream);
    }

    private static byte[] UseStreamDotReadMethod(Stream stream)
    {
        List<byte> totalStream = new();
        var buffer = new byte[32];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalStream.AddRange(buffer.Take(read));
        }

        return totalStream.ToArray();
    }

    private static string GetValueOfField(IDictionary<string, object> fields, string key)
    {
        if (fields.TryGetValue(key, out var value))
        {
            return value as string ?? string.Empty;
        }

        return string.Empty;
    }
}
