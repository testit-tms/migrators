using AllureExporter.Models;

namespace AllureExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachmentsforTestCase(int testCaseId, Guid id);
    Task<List<string>> DownloadAttachmentsforSharedStep(int sharedStepId, Guid id);
}
