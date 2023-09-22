using AzureExporter.Client;
using AzureExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AzureExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;
    private List<WorkItemRelation> _anotherRelations;
    private List<WorkItemRelation> _relations;
    private List<string> _expectedNames;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
        _anotherRelations = new List<WorkItemRelation>();
        _relations = new List<WorkItemRelation>();
        _expectedNames = new List<string>();

        for (int i = 0; i < 10; i++) {
            _anotherRelations.Add(
                new WorkItemRelation
                {
                    Rel = "Another" + i,
                    Attributes = new Dictionary<string, object>(),
                });
        }

        for (int i = 0; i < 10; i++)
        {
            var name = "name" + i;
            var attributes = new Dictionary<string, object>();

            attributes.Add("name", name);
            _expectedNames.Add(name);

            _relations.Add(
                new WorkItemRelation
                {
                    Attributes = attributes,
                    Rel = "AttachedFile",
                    Url = "https://dev.azure.com/" + new Guid()
                });
        }
    }

    [Test]
    public async Task DownloadAttachments_FailedGetAttachments()
    {
        // Arrange
        _client.GetAttachmentById(new Guid())
            .ThrowsAsync(new Exception("Failed to download attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(_relations, new Guid()));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task DownloadAttachments_FailedWriteAttachment()
    {
        // Arrange
        var guid = new Guid();
        var bytes = new byte[] { 1, 2, 3 };
        _client.GetAttachmentById(new Guid()).Returns(bytes);
        _writeService.WriteAttachment(guid, bytes, Arg.Any<string>())
            .Throws(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(_relations, guid));
    }

    [Test]
    public async Task DownloadAttachments_ShouldReturnEmptyList_WhenWorkItemRelationsIsEmpty()
    {
        // Arrange
        var guid = new Guid();
        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await attachmentService.DownloadAttachments(
            new List<WorkItemRelation>(),
            guid
            );

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task DownloadAttachments_ShouldReturnEmptyList_WhenWorkItemRelationsHasAnotherRelations()
    {
        // Arrange
        var guid = new Guid();
        var attachmentService = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await attachmentService.DownloadAttachments(
            _anotherRelations,
            guid
            );

        // Assert
        Assert.That(result, Is.Empty);
    }

     [Test]
     public async Task DownloadAttachments_ShouldReturnNames_WhenWorkItemRelationsHasRelations()
     {
         // Arrange
         var guid = new Guid();
         var bytes = new byte[] { 1, 2, 3 };

         _client.GetAttachmentById(new Guid())
             .Returns(bytes);

         foreach (var name in _expectedNames)
         {
             _writeService.WriteAttachment(guid, bytes, name).Returns(name);
         }

         var attachmentService = new AttachmentService(_logger, _client, _writeService);

         // Act
         var result = await attachmentService.DownloadAttachments(
             _relations,
             guid
         );

         // Assert
         for (int i = 0; i < result.Count; i++)
         {
             Assert.That(result[i], Is.EqualTo(_expectedNames[i]));
         }
     }

}
