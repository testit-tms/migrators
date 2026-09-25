using DoqaExporter.Client;
using DoqaExporter.Models;
using DoqaExporter.Services;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DoqaExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger = null!;
    private IDoqaClient _client = null!;
    private IWriteService _writeService = null!;
    private DoqaConfig _config = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExportService>>();
        _client = Substitute.For<IDoqaClient>();
        _writeService = Substitute.For<IWriteService>();
        _config = new DoqaConfig
        {
            Url = "https://example.doqa.app",
            Email = "user@example.com",
            Password = "secret",
            SpaceId = 1
        };
    }

    private ExportService CreateSut() => new(_client, _writeService, _config, _logger);

    private static DoqaFolderTree EmptyFolders() => new()
    {
        Data = new DoqaFolderData { Id = 0, Name = "root" },
        Children = new List<DoqaFolderTree>()
    };

    private static DoqaFolderTree FoldersWithOneSection() => new()
    {
        Data = new DoqaFolderData { Id = 10, Name = "Root" },
        Children = new List<DoqaFolderTree>
        {
            new()
            {
                Data = new DoqaFolderData { Id = 20, Name = "Folder A" },
                Children = new List<DoqaFolderTree>()
            }
        }
    };

    private void ArrangeHappyPathBasics()
    {
        _client.Login().Returns("token");
        _client.GetProjects().Returns(new List<DoqaProject>
        {
            new() { Id = 1, Name = "Demo Project" }
        });
        _client.GetFolders(_config.SpaceId).Returns(FoldersWithOneSection());
        _client.GetAllCaseIds(_config.SpaceId).Returns(new List<int>());
        _client.GetAllChecklistIds(_config.SpaceId).Returns(new List<int>());
    }

    [Test]
    public async Task ExportProject_FailedLogin()
    {
        _client.Login().Throws(new Exception("Failed to login"));

        var sut = CreateSut();

        Assert.ThrowsAsync<Exception>(async () => await sut.ExportProject());

        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.DidNotReceive().WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_Empty_WritesMainJson()
    {
        ArrangeHappyPathBasics();

        var sut = CreateSut();
        await sut.ExportProject();

        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.Received(1).WriteMainJson(Arg.Is<Root>(r =>
            r.ProjectName == "Demo Project" &&
            r.TestCases.Count == 0));
    }

    [Test]
    public async Task ExportProject_Success_WritesCaseAndChecklist()
    {
        ArrangeHappyPathBasics();

        _client.GetAllCaseIds(_config.SpaceId).Returns(new List<int> { 100 });
        _client.GetCase(100).Returns(new DoqaTestCase
        {
            Id = 100,
            Title = "Case 1",
            FolderId = 20,
            Status = "ready",
            Priority = "high",
            Steps = new List<DoqaStep>
            {
                new() { StepText = "Do something", Result = "OK" }
            }
        });

        _client.GetAllChecklistIds(_config.SpaceId).Returns(new List<int> { 200 });
        _client.GetChecklist(200).Returns(new DoqaChecklist
        {
            Id = 200,
            Title = "CL 1",
            Status = "ready",
            Priority = "medium",
            Children = new List<DoqaChecklistItem>
            {
                new() { Title = "Item 1", Children = new List<DoqaChecklistItem>() }
            }
        });

        var sut = CreateSut();
        await sut.ExportProject();

        await _writeService.Received(2).WriteTestCase(Arg.Any<TestCase>());
        await _writeService.Received(1).WriteTestCase(Arg.Is<TestCase>(tc =>
            tc.Name == "Case 1" && tc.Priority == PriorityType.High));
        await _writeService.Received(1).WriteTestCase(Arg.Is<TestCase>(tc =>
            tc.Name == "[Чеклист] CL 1" && tc.Tags.Contains("checklist")));
        await _writeService.Received(1).WriteMainJson(Arg.Is<Root>(r =>
            r.TestCases.Count == 2 &&
            r.Sections.Any(s => s.Name == "Чеклисты")));
    }

    [Test]
    public async Task ExportProject_GetCaseFails_ContinuesAndWritesMainJson()
    {
        ArrangeHappyPathBasics();

        _client.GetAllCaseIds(_config.SpaceId).Returns(new List<int> { 100 });
        _client.GetCase(100).Throws(new Exception("Failed to get case"));

        var sut = CreateSut();
        await sut.ExportProject();

        await _writeService.DidNotReceive().WriteTestCase(Arg.Any<TestCase>());
        await _writeService.Received(1).WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteMainJson()
    {
        ArrangeHappyPathBasics();
        _client.GetFolders(_config.SpaceId).Returns(EmptyFolders());

        _writeService.WriteMainJson(Arg.Any<Root>())
            .Throws(new Exception("Failed to write main.json"));

        var sut = CreateSut();

        Assert.ThrowsAsync<Exception>(async () => await sut.ExportProject());
    }

    [Test]
    public async Task ExportProject_UntitledChecklist_UsesDefaultName()
    {
        ArrangeHappyPathBasics();

        _client.GetAllChecklistIds(_config.SpaceId).Returns(new List<int> { 1 });
        _client.GetChecklist(1).Returns(new DoqaChecklist
        {
            Id = 1,
            Title = "",
            Children = new List<DoqaChecklistItem>()
        });

        var sut = CreateSut();
        await sut.ExportProject();

        await _writeService.Received(1).WriteTestCase(Arg.Is<TestCase>(tc =>
            tc.Name == "[Чеклист] "));
    }
}
