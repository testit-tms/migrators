using HPALMExporter.Models;

namespace HPALMExporter.Services;

public interface IAttachmentService
{
    Task<AttachmentData> ConvertAttachmentsFromTest(Guid testCaseId, int testId);
    Task<AttachmentData> ConvertAttachmentsFromStep(Guid testCaseId, int stepId);
}
