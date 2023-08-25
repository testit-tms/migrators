using AzureExporter.Models;

namespace AzureExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachments(List<int> attachments);
}
