using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services.Helpers;

namespace ZephyrScaleServerExporter.Services.TestCase.Implementations;

public class TestCaseAttachmentsService(
    IClient client,
    IOptions<AppConfig> config,
    IAttachmentService attachmentService)
    : ITestCaseAttachmentsService
{

    public async Task<List<string>> FillAttachments( Guid testCaseId,
        ZephyrTestCase zephyrTestCase,
        ZephyrDescriptionData description)
    {

        var attachments = new List<string>();
        // API Call
        try
        {
            List<ZephyrAttachment> zephyrAttachments;
            if (zephyrTestCase is { IsArchived: true, JiraId: not null })
            {
                var altAttachmentsForTestCase = await client.
                    GetAltAttachmentsForTestCase(zephyrTestCase.JiraId);
                zephyrAttachments = altAttachmentsForTestCase.Select(x =>
                    x.ToZephyrAttachment(config)).ToList();
            }
            else
            {
                zephyrAttachments = await client.GetAttachmentsForTestCase(zephyrTestCase.Key!);
            }

            List<ZephyrAttachment> toDownloadList = [];
            Utils.AddIfUnique(toDownloadList, zephyrAttachments);
            Utils.AddIfUnique(toDownloadList, description.Attachments);

            var tasks = toDownloadList
                .AsParallel()
                .WithDegreeOfParallelism(Utils.GetLogicalProcessors())
                .Select(async x =>
                    await attachmentService.DownloadAttachment(testCaseId, x, false)
                ).ToList();
            var res = await Task.WhenAll(tasks);
            Utils.AddIfUnique(attachments, res.ToList());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return attachments;
    }

    public async Task<List<string>> CalcPreconditionAttachments(Guid testCaseId, ZephyrDescriptionData precondition,
        List<string> attachments)
    {
        var preconditionAttachments = new List<string>();
        foreach (var attachment in precondition.Attachments)
        {
            var fileName = await attachmentService.DownloadAttachment(testCaseId, attachment, false);
            Utils.AddIfUnique(preconditionAttachments, fileName);
            Utils.AddIfUnique(attachments, fileName);
        }
        return preconditionAttachments;
    }
}
