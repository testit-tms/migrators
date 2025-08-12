using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services.Helpers;

namespace ZephyrScaleServerExporter.Services.Implementations;

internal class AttachmentService(
    IDetailedLogService detailedLogService,
    IWriteService writeService, 
    IClient client)
    : IAttachmentService
{
    /// <summary>
    /// Using cached fileName:filePath dictionary, copying bytes for given fileNames 
    /// from step attachments lists to targetId's testCase destination. 
    /// Returns resultList of copyied fileNames. 
    /// Allow second call for the same values (skip copy in this case, return fileNames as before for existing).
    /// </summary>
    public async Task<List<string>> CopySharedAttachments(Guid targetId, Step step)
    {
        var all = step.GetAllAttachments();
        var resultList = new List<string>();
        foreach (string fileName in all) {
            var res = await writeService.CopyAttachment(targetId, fileName);
            if (res != null) resultList.Add(res);
        }
        return resultList;
    }

    public async Task<string> DownloadAttachment(Guid testCaseId, ZephyrAttachment attachment, bool isSharedAttachment)
    {
        detailedLogService.LogDebug("Downloading attachment {@Attachment}", attachment);
        var bytes = await client.DownloadAttachment(attachment.Url, testCaseId);
        attachment.FileName = Utils.SpacesToUnderscores(attachment.FileName);
        attachment.FileName = Utils.ReplaceInvalidChars(attachment.FileName);
        return await writeService.WriteAttachment(testCaseId, bytes, attachment.FileName, isSharedAttachment);
    }

    public async Task<string> DownloadAttachmentById(Guid testCaseId, StepAttachment attachment, bool isSharedAttachment)
    {
        detailedLogService.LogDebug("Downloading attachment by id {@Attachment}", attachment);
        var bytes = await client.DownloadAttachmentById(attachment.Id, testCaseId);
        // use Name instead of FileName because FileName is synthetic there
        attachment.Name = Utils.SpacesToUnderscores(attachment.Name!);
        attachment.Name = Utils.ReplaceInvalidChars(attachment.Name);
        return await writeService.WriteAttachment(testCaseId, bytes, attachment.Name, isSharedAttachment);
    }

}
