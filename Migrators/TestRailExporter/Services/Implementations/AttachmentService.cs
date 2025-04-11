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
            if (attachmentsMap.ContainsKey(attachment.Id.ToString()) || attachmentsMap.ContainsKey(attachment.Guid))
            {
                logger.LogDebug("Attachment with id {Id} and guid {Guid} has already been added to attachment map: {@AttachmentsMap}",
                    attachment.Id, attachment.Guid, attachmentsMap);

                continue;
            }

            logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await client.GetAttachmentById(attachment.Id);
            var name = await writeService.WriteAttachment(id, bytes, CorrectAttachmentName(attachment.Name));

            names.Add(name);
            attachmentsMap.Add(attachment.Id.ToString(), name);
            attachmentsMap.Add(attachment.Guid, name);
        }

        logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return new AttachmentsInfo
        {
            AttachmentNames = names,
            AttachmentsMap = attachmentsMap,
        };
    }

    public async Task<string> DownloadAttachmentById(int attachmentId, Guid id)
    {
        logger.LogInformation("Downloading attachment by id {Id}", attachmentId);

        var bytes = await client.GetAttachmentById(attachmentId);
        var attahmentName = Guid.NewGuid().ToString() + "-attachment";
        var name = await writeService.WriteAttachment(id, bytes, attahmentName);

        logger.LogDebug("Ending downloading attachment: {Name}", name);

        return name;
    }

    private string CorrectAttachmentName(string name)
    {
        return _symbolsToReplaceRegex.Replace(name, "_");
    }
}
