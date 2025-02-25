using TestRailExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using TestRailExporter.Models;

namespace TestRailExporter.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ILogger<AttachmentService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
    private static readonly Regex _symbolsToReplaceRegex = new Regex("[\\/:*?\"<>|]");

    public AttachmentService(ILogger<AttachmentService> logger, IClient client, IWriteService writeService)
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
    }

    public async Task<AttachmentsInfo> DownloadAttachmentsByCaseId(int testCaseId, Guid id)
    {
        _logger.LogInformation("Downloading attachments by test case id {Id}", testCaseId);

        var attachments = await _client.GetAttachmentsByTestCaseId(testCaseId);

        _logger.LogDebug("Found attachments: {@Attachments}", attachments);

        var names = new List<string>();
        var attachmentsMap = new Dictionary<int, string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await _client.GetAttachmentById(attachment.Id);
            var name = await _writeService.WriteAttachment(id, bytes, CorrectAttachmentName(attachment.Name));

            names.Add(name);
            attachmentsMap.Add(attachment.Id, name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return new AttachmentsInfo
        {
            AttachmentNames = names,
            AttachmentsMap = attachmentsMap,
        };
    }

    public async Task<string> DownloadAttachmentById(int attachmentId, Guid id)
    {
        _logger.LogInformation("Downloading attachment by id {Id}", attachmentId);

        var bytes = await _client.GetAttachmentById(attachmentId);
        var attahmentName = Guid.NewGuid().ToString() + "-attachment";
        var name = await _writeService.WriteAttachment(id, bytes, attahmentName);

        _logger.LogDebug("Ending downloading attachment: {Name}", name);

        return name;
    }

    private string CorrectAttachmentName(string name)
    {
        return _symbolsToReplaceRegex.Replace(name, "_");
    }
}
