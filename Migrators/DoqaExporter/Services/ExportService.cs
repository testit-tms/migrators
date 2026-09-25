using DoqaExporter.Client;
using DoqaExporter.Models;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace DoqaExporter.Services;

public class ExportService : IExportService
{
    private readonly IDoqaClient _client;
    private readonly IWriteService _writeService;
    private readonly DoqaConfig _config;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IDoqaClient client,
        IWriteService writeService,
        DoqaConfig config,
        ILogger<ExportService> logger)
    {
        _client = client;
        _writeService = writeService;
        _config = config;
        _logger = logger;
    }

    public async Task ExportProject()
    {
        _logger.LogInformation("Начало экспорта из DOQA");

        // 1. Авторизация
        await _client.Login();

        // 2. Проекты
        var projects = await _client.GetProjects();
        var projectName = projects.FirstOrDefault()?.Name ?? "DOQA Export";
        _logger.LogInformation("Проект: {ProjectName}", projectName);

        // 3. Папки → секции
        _logger.LogInformation("Загрузка структуры папок (space {SpaceId})...", _config.SpaceId);
        var folderTree = await _client.GetFolders(_config.SpaceId);
        var (sections, folderToSection) = BuildSections(folderTree);
        _logger.LogInformation("Создано {Count} секций", sections.Count);

        // 4. Загрузка кейсов
        _logger.LogInformation("Загрузка ID кейсов...");
        var caseIds = await _client.GetAllCaseIds(_config.SpaceId);

        var testCaseGuids = new List<Guid>();
        var errors = 0;

        for (var i = 0; i < caseIds.Count; i++)
        {
            try
            {
                var doqaCase = await _client.GetCase(caseIds[i]);
                var (guid, testCase) = await ConvertCase(doqaCase, folderToSection);
                testCaseGuids.Add(guid);
                await _writeService.WriteTestCase(testCase);

                if ((i + 1) % 50 == 0)
                    _logger.LogInformation("  Экспортировано {Current}/{Total} кейсов...", i + 1, caseIds.Count);

                await Task.Delay(200); // Rate limiting
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError("Ошибка экспорта кейса {CaseId}: {Message}", caseIds[i], ex.Message);
            }
        }

        // 5. Чеклисты → тест-кейсы
        _logger.LogInformation("Загрузка чеклистов...");
        var checklistIds = await _client.GetAllChecklistIds(_config.SpaceId);

        // Добавляем секцию "Чеклисты"
        var checklistSectionId = Guid.NewGuid();
        sections.Add(new Section
        {
            Id = checklistSectionId,
            Name = "Чеклисты",
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        });

        var checklistErrors = 0;
        for (var i = 0; i < checklistIds.Count; i++)
        {
            try
            {
                var checklist = await _client.GetChecklist(checklistIds[i]);
                var (guid, testCase) = ConvertChecklist(checklist, checklistSectionId);
                testCaseGuids.Add(guid);
                await _writeService.WriteTestCase(testCase);

                if ((i + 1) % 20 == 0)
                    _logger.LogInformation("  Экспортировано {Current}/{Total} чеклистов...", i + 1, checklistIds.Count);

                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                checklistErrors++;
                _logger.LogError("Ошибка экспорта чеклиста {Id}: {Message}", checklistIds[i], ex.Message);
            }
        }

        _logger.LogInformation("Чеклисты: {Exported}/{Total}, {Errors} ошибок",
            checklistIds.Count - checklistErrors, checklistIds.Count, checklistErrors);

        // 6. Main.json
        var root = new Root
        {
            ProjectName = projectName,
            Sections = sections,
            TestCases = testCaseGuids,
            SharedSteps = new List<Guid>(),
            Attributes = new List<global::Models.Attribute>()
        };
        await _writeService.WriteMainJson(root);

        _logger.LogInformation(
            "Экспорт завершён: {CasesExported} кейсов + {ChecklistsExported} чеклистов, {Errors} ошибок",
            caseIds.Count - errors, checklistIds.Count - checklistErrors, errors + checklistErrors);
    }

    // -- Секции из папок ---------------------------------------------------

    private static (List<Section>, Dictionary<int, Guid>) BuildSections(DoqaFolderTree tree)
    {
        var folderToSection = new Dictionary<int, Guid>();

        Section? ProcessNode(DoqaFolderTree node)
        {
            var data = node.Data;
            if (data.Id == 0) return null;

            var guid = Guid.NewGuid();
            folderToSection[data.Id] = guid;

            var children = node.Children
                .Select(ProcessNode)
                .Where(s => s != null)
                .Select(s => s!)
                .ToList();

            return new Section
            {
                Id = guid,
                Name = data.Name,
                PreconditionSteps = new List<Step>(),
                PostconditionSteps = new List<Step>(),
                Sections = children
            };
        }

        var rootSection = ProcessNode(tree);
        if (rootSection == null)
            return (new List<Section>(), folderToSection);

        // Корень без вложенных + дочерние как top-level
        var sections = rootSection.Sections.ToList();
        sections.Insert(0, new Section
        {
            Id = rootSection.Id,
            Name = rootSection.Name,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        });

        return (sections, folderToSection);
    }

    // -- Конвертация кейса ------------------------------------------------

    private async Task<(Guid, TestCase)> ConvertCase(
        DoqaTestCase doqa,
        Dictionary<int, Guid> folderToSection)
    {
        var guid = Guid.NewGuid();
        var imageCounter = 0;

        // Секция
        var sectionId = folderToSection.TryGetValue(doqa.FolderId, out var sid)
            ? sid
            : folderToSection.Values.FirstOrDefault();

        // Приоритет и статус
        var priority = doqa.Priority?.ToLower() switch
        {
            "critical" => PriorityType.Highest,
            "high" => PriorityType.High,
            "medium" => PriorityType.Medium,
            "low" => PriorityType.Low,
            "trivial" => PriorityType.Lowest,
            _ => PriorityType.Medium
        };

        var state = doqa.Status?.ToLower() switch
        {
            "ready" or "actual" => StateType.Ready,
            "draft" => StateType.NeedsWork,
            "deprecated" => StateType.NotReady,
            _ => StateType.Ready
        };

        // Шаги
        var steps = new List<Step>();
        for (var i = 0; i < doqa.Steps.Count; i++)
        {
            var step = doqa.Steps[i];
            var actionResult = HtmlProcessor.Process(step.StepText, $"step{i + 1}_action", ref imageCounter);
            var expectedResult = HtmlProcessor.Process(step.Result, $"step{i + 1}_expected", ref imageCounter);

            // Сохранить картинки
            var actionAttachments = await SaveImages(guid, actionResult.Images);
            var expectedAttachments = await SaveImages(guid, expectedResult.Images);

            steps.Add(new Step
            {
                Action = actionResult.Text,
                Expected = expectedResult.Text,
                TestData = step.TestData ?? string.Empty,
                ActionAttachments = actionAttachments,
                ExpectedAttachments = expectedAttachments,
                TestDataAttachments = new List<string>()
            });
        }

        // Предусловия
        var preconditionSteps = new List<Step>();
        var precondResult = HtmlProcessor.Process(doqa.Preconditions, "precond", ref imageCounter);
        if (!string.IsNullOrEmpty(precondResult.Text))
        {
            var precondAttachments = await SaveImages(guid, precondResult.Images);
            preconditionSteps.Add(new Step
            {
                Action = precondResult.Text,
                Expected = string.Empty,
                TestData = string.Empty,
                ActionAttachments = precondAttachments,
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            });
        }

        // Описание
        var descHtml = doqa.Description ?? string.Empty;
        if (!string.IsNullOrEmpty(doqa.ExpectedResult))
            descHtml += $"\n\nОжидаемый результат: {doqa.ExpectedResult}";
        var descResult = HtmlProcessor.Process(descHtml, "desc", ref imageCounter);
        var descAttachments = await SaveImages(guid, descResult.Images);

        // Ссылки — из всех HTML-полей
        var allHtml = string.Join("\n", new[]
        {
            doqa.Description, doqa.Preconditions, doqa.ExpectedResult
        }.Concat(doqa.Steps.Select(s => $"{s.StepText} {s.Result}")));
        var links = HtmlProcessor.ExtractLinks(allHtml)
            .Select(l => new Link { Url = l.Url, Title = l.Title, Description = l.Description, Type = l.Type })
            .ToList();

        // Файловые аттачи
        var fileAttachments = new List<string>();
        foreach (var att in doqa.Attachments)
        {
            if (string.IsNullOrEmpty(att.Path)) continue;
            var data = await _client.DownloadAttachment(att.Path);
            if (data != null)
            {
                var fileName = await _writeService.WriteAttachment(guid, data, att.OriginalName);
                fileAttachments.Add(fileName);
            }
        }

        var testCase = new TestCase
        {
            Id = guid,
            Name = string.IsNullOrEmpty(doqa.Title) ? "Без названия" : doqa.Title[..Math.Min(doqa.Title.Length, 255)],
            SectionId = sectionId,
            Description = descResult.Text,
            State = state,
            Priority = priority,
            Duration = 0,
            Steps = steps,
            PreconditionSteps = preconditionSteps,
            PostconditionSteps = new List<Step>(),
            Attributes = new List<CaseAttribute>(),
            Tags = new List<string>(),
            Attachments = descAttachments.Concat(fileAttachments).ToList(),
            Iterations = new List<Iteration>(),
            Links = links
        };

        return (guid, testCase);
    }

    private async Task<List<string>> SaveImages(Guid caseId, List<HtmlProcessor.ExtractedImage> images)
    {
        var fileNames = new List<string>();
        foreach (var img in images)
        {
            var fileName = await _writeService.WriteAttachment(caseId, img.Data, img.FileName);
            fileNames.Add(fileName);
        }
        return fileNames;
    }

    // -- Чеклисты → тест-кейсы -------------------------------------------

    private (Guid, TestCase) ConvertChecklist(DoqaChecklist checklist, Guid sectionId)
    {
        var guid = Guid.NewGuid();
        var imageCounter = 0;

        var priority = checklist.Priority?.ToLower() switch
        {
            "critical" => PriorityType.Highest,
            "high" => PriorityType.High,
            "low" => PriorityType.Low,
            _ => PriorityType.Medium
        };

        var state = checklist.Status?.ToLower() switch
        {
            "ready" or "actual" => StateType.Ready,
            "draft" => StateType.NeedsWork,
            _ => StateType.Ready
        };

        // Дерево пунктов → плоский список шагов
        var steps = new List<Step>();
        FlattenChecklistItems(checklist.Children, steps, 0, ref imageCounter);

        // Описание
        var descResult = HtmlProcessor.Process(checklist.Description, "desc", ref imageCounter);

        // Ссылки из описания
        var links = HtmlProcessor.ExtractLinks(checklist.Description)
            .Select(l => new Link { Url = l.Url, Title = l.Title, Description = l.Description, Type = l.Type })
            .ToList();

        var testCase = new TestCase
        {
            Id = guid,
            Name = $"[Чеклист] {(string.IsNullOrEmpty(checklist.Title) ? "Без названия" : checklist.Title)}"[..Math.Min($"[Чеклист] {checklist.Title}".Length, 255)],
            SectionId = sectionId,
            Description = descResult.Text,
            State = state,
            Priority = priority,
            Duration = 0,
            Steps = steps,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Attributes = new List<CaseAttribute>(),
            Tags = new List<string> { "checklist" },
            Attachments = new List<string>(),
            Iterations = new List<Iteration>(),
            Links = links,
        };

        return (guid, testCase);
    }

    private void FlattenChecklistItems(
        List<DoqaChecklistItem> items, List<Step> steps, int depth, ref int imageCounter)
    {
        foreach (var item in items)
        {
            var title = item.Title;
            if (string.IsNullOrWhiteSpace(title)) continue;

            var processed = HtmlProcessor.Process(title, $"cl_step{steps.Count + 1}", ref imageCounter);
            var indent = depth > 0 ? new string(' ', depth * 2) + "↳ " : "";

            steps.Add(new Step
            {
                Action = indent + processed.Text,
                Expected = string.Empty,
                TestData = string.Empty,
                ActionAttachments = new List<string>(),
                ExpectedAttachments = new List<string>(),
                TestDataAttachments = new List<string>()
            });

            if (item.Children.Count > 0)
                FlattenChecklistItems(item.Children, steps, depth + 1, ref imageCounter);
        }
    }
}
