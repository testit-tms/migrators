using TestLinkExporter.Models;

namespace TestLinkExporter.Services;

public interface IAttachmentService
{
    List<string> DownloadAttachments(int Id, Guid workItemId);
}
