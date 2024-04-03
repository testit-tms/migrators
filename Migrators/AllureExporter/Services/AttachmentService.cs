using AllureExporter.Client;
using JsonWriter;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AllureExporter.Services;

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

    public async Task<List<string>> DownloadAttachmentsforTestCase(int testCaseId, Guid id)
    {
        _logger.LogInformation("Downloading attachments by test case id {Id}", testCaseId);

        var attachments = await _client.GetAttachmentsByTestCaseId(testCaseId);

        _logger.LogDebug("Found attachments: {@Attachments}", attachments);

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await _client.DownloadAttachmentForTestCase(attachment.Id);
            var name = await _writeService.WriteAttachment(id, bytes, CorrectAttachmentName(attachment.Name));
            names.Add(name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }

    public async Task<List<string>> DownloadAttachmentsforSharedStep(int sharedStepId, Guid id)
    {
        _logger.LogInformation("Downloading attachments by shared step id {Id}", sharedStepId);

        var attachments = await _client.GetAttachmentsBySharedStepId(sharedStepId);

        _logger.LogDebug("Found attachments: {@Attachments}", attachments);

        var names = new List<string>();

        foreach (var attachment in attachments)
        {
            _logger.LogDebug("Downloading attachment: {Name}", attachment.Name);

            var bytes = await _client.DownloadAttachmentForSharedStep(attachment.Id);
            var name = await _writeService.WriteAttachment(id, bytes, CorrectAttachmentName(attachment.Name));
            names.Add(name);
        }

        _logger.LogDebug("Ending downloading attachments: {@Names}", names);

        return names;
    }

    private string CorrectAttachmentName(string name)
    {
        return _symbolsToReplaceRegex.Replace(name, "_");
    }
}
