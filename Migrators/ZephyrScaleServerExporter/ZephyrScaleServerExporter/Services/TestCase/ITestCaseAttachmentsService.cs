using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Services.TestCase;

public interface ITestCaseAttachmentsService
{
    Task<List<string>> CalcPreconditionAttachments(Guid testCaseId, ZephyrDescriptionData precondition,
        List<string> attachments);

    Task<List<string>> FillAttachments(Guid testCaseId,
        ZephyrTestCase zephyrTestCase,
        ZephyrDescriptionData description);
}