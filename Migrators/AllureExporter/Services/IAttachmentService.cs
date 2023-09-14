using AllureExporter.Models;

namespace AllureExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachments(int testCaseId, Guid id);
}
