using AzureExporter.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachments(List<WorkItemRelation> attachments, Guid workItemId);
}
