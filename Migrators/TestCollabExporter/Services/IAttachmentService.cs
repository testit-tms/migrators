namespace TestCollabExporter.Services;

public interface IAttachmentService
{
    Task<string> DownloadAttachment(Guid testCase, string link, string filename);
}
