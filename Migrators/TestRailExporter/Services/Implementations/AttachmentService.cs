using TestRailExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services.Implementations;

public class AttachmentService(ILogger<AttachmentService> logger, IClient client, IWriteService writeService)
    : IAttachmentService
{
    private static readonly Regex _symbolsToReplaceRegex = new Regex("[\\/:*?\"<>|]");

    public async Task<AttachmentsInfo> DownloadAttachmentsByCaseId(int testCaseId, Guid id)
    {
        logger.LogInformation("Downloading attachments by test case id {Id}", testCaseId);

        var attachments = await client.GetAttachmentsByTestCaseId(testCaseId);

        logger.LogDebug("Found attachments: {@Attachments}", attachments);

        var names = new List<string>();
        var attachmentsMap = new Dictionary<string, string>();

        foreach (var attachment in attachments)
        {
            if (attachmentsMap.ContainsKey(attachment.Id) ||
                (!string.IsNullOrEmpty(attachment.Guid) && attachmentsMap.ContainsKey(attachment.Guid)))
            {
                logger.LogDebug("Attachment with id {Id} and guid {Guid} has already been added to attachment map: {@AttachmentsMap}",
                    attachment.Id, attachment.Guid, attachmentsMap);

                continue;
            }

            logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await DownloadAttachmentWithFallback(attachment.Id, attachment.Guid);
            if (bytes.Length == 0)
            {
                logger.LogWarning("Failed to download attachment by id {AttachmentId}", attachment.Id);
                continue;
            }

            var name = await writeService.WriteAttachment(id, bytes, CorrectAttachmentName(attachment.Name));

            names.Add(name);
            attachmentsMap[attachment.Id] = name;
            if (!string.IsNullOrEmpty(attachment.Guid))
                attachmentsMap[attachment.Guid] = name;
        }

        logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return new AttachmentsInfo
        {
            AttachmentNames = names,
            AttachmentsMap = attachmentsMap,
        };
    }

    public async Task<string> DownloadAttachmentById(string attachmentId, Guid id)
    {
        logger.LogInformation("Downloading attachment by id {Id}", attachmentId);

        var bytes = await DownloadAttachmentWithFallback(attachmentId);
        if (bytes.Length == 0)
            return string.Empty;

        var attachmentName = Guid.NewGuid().ToString() + "-attachment";
        var name = await writeService.WriteAttachment(id, bytes, attachmentName);

        logger.LogDebug("Ending downloading attachment: {Name}", name);

        return name;
    }

    private async Task<byte[]> DownloadAttachmentWithFallback(string attachmentId, string? alternateId = null)
    {
        var bytes = await client.GetAttachmentById(attachmentId);
        if (bytes.Length > 0)
            return bytes;

        if (!string.IsNullOrEmpty(alternateId) && alternateId != attachmentId)
        {
            logger.LogDebug("Retrying attachment download with alternate id {AlternateId}", alternateId);
            bytes = await client.GetAttachmentById(alternateId);
            if (bytes.Length > 0)
                return bytes;
        }

        if (int.TryParse(attachmentId, out var legacyId))
        {
            logger.LogDebug("Retrying attachment download using legacy numeric id {LegacyId}", legacyId);
            bytes = await client.GetAttachmentById(legacyId);
            if (bytes.Length > 0)
                return bytes;
        }

        if (!string.IsNullOrEmpty(alternateId) && int.TryParse(alternateId, out var alternateLegacyId))
        {
            logger.LogDebug("Retrying attachment download using legacy numeric alternate id {LegacyId}", alternateLegacyId);
            bytes = await client.GetAttachmentById(alternateLegacyId);
        }

        return bytes;
    }

    private string CorrectAttachmentName(string name)
    {
        return _symbolsToReplaceRegex.Replace(name, "_");
    }
}
