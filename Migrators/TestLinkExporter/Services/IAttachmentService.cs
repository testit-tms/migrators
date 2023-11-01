using TestLinkExporter.Models;

namespace TestLinkExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachments(int Id, Guid workItemId);
}
