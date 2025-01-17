using AllureExporter.Models;

namespace AllureExporter.Services;

public interface IAttachmentService
{
    Task<List<string>> DownloadAttachmentsforTestCase(long testCaseId, Guid id);
    Task<List<string>> DownloadAttachmentsforSharedStep(long sharedStepId, Guid id);
}
