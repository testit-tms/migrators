using System.Text.RegularExpressions;
using AllureExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;

namespace AllureExporter.Services.Implementations;

internal class AttachmentService(ILogger<AttachmentService> logger, IClient client, IWriteService writeService)
    : IAttachmentService
{
    private static readonly Regex _symbolsToReplaceRegex = new("[\\/:*?\"<>|]");

    public async Task<List<string>> DownloadAttachmentsforTestCase(long testCaseId, Guid id)
    {
        logger.LogInformation("Downloading attachments by test case id {Id}", testCaseId);

        var attachments = await client.GetAttachmentsByTestCaseId(testCaseId);

        logger.LogDebug("Found attachments: {@Attachments}", attachments);

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await client.DownloadAttachmentForTestCase(attachment.Id);
            var name = await writeService.WriteAttachment(id, bytes, CorrectAttachmentName(attachment.Name));
            names.Add(name);
        }

        logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }

    public async Task<List<string>> DownloadAttachmentsforSharedStep(long sharedStepId, Guid id)
    {
        logger.LogInformation("Downloading attachments by shared step id {Id}", sharedStepId);

        var attachments = await client.GetAttachmentsBySharedStepId(sharedStepId);

        logger.LogDebug("Found attachments: {@Attachments}", attachments);

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await client.DownloadAttachmentForSharedStep(attachment.Id);
            var name = await writeService.WriteAttachment(id, bytes, CorrectAttachmentName(attachment.Name));
            names.Add(name);
        }

        logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }

    private string CorrectAttachmentName(string name)
    {
        return _symbolsToReplaceRegex.Replace(name, "_");
    }
}
