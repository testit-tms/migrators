using System.Text.Json;
using TestRailExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using System.Text.RegularExpressions;
using TestRailExporter.Models.Client;
using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services.Implementations;

public class StepService(ILogger<StepService> logger, IClient client, IAttachmentService attachmentService)
    : IStepService
{
    private readonly IClient _client = client;
    private AttachmentsInfo _attachmentsInfo = new();
    private static readonly Regex _ImgRegex = new Regex(@"!\[\]\(([^)]*)\)");
    private static readonly Regex _HyperlinkRegex = new Regex(@"\[[^\[\]]*\]\([^()\s]*\)");
    private static readonly Regex _UrlRegex = new Regex(@"\(([^()\s]+)\)");
    private static readonly Regex _TitleRegex = new Regex(@"\[([^\[\]]+)\]");
    private static readonly Regex _TableRegex = new Regex(@"(\|{2,}[^\n]*\n)+");
    private static readonly Regex _CellRegex = new Regex(@"\|([^\|\n]+)");

    public async Task<List<Step>> ConvertStepsForTestCase(TestRailCase testCase, Guid testCaseId, Dictionary<int, SharedStep> sharedStepMap, AttachmentsInfo attachmentsInfo)
    {
        logger.LogDebug("Converting steps for test case {Name}", testCase.Title);

        _attachmentsInfo = attachmentsInfo;

        if (testCase.Steps != null)
        {
            return await ConvertListStepsForTestCase(testCase.Steps, testCaseId, sharedStepMap);
        }
        else if (testCase.TextSteps != null || testCase.TextExpected != null)
        {
            var step = await ConvertStep(testCase.TextSteps, testCase.TextExpected, testCaseId);

            return new List<Step> { step };
        }
        else if (testCase.TextMission != null || testCase.TextGoals != null)
        {
            var step = await ConvertStep(testCase.TextMission, testCase.TextGoals, testCaseId);

            return new List<Step> { step };
        }
        else if (testCase.TextScenarios != null)
        {
            var scenarios = JsonSerializer.Deserialize<List<TestRailScenario>>(testCase.TextScenarios)!;

            return await ConvertScenariosForTestCase(scenarios, testCaseId);
        }

        return new List<Step>();
    }

    public async Task<StepsInfo> ConvertStepsForSharedStep(TestRailSharedStep sharedStep, Guid sharedStepId)
    {
        logger.LogDebug("Converting steps for shared step {Name}", sharedStep.Title);

        var steps = new List<Step>();
        var attachmentNames = new List<string>();

        foreach (var testRailStep in sharedStep.Steps)
        {
            var step = await ConvertStep(testRailStep.Action, testRailStep.Expected, sharedStepId);

            steps.Add(step);
            attachmentNames.AddRange(step.ActionAttachments);
            attachmentNames.AddRange(step.ExpectedAttachments);
        }

        return new StepsInfo
        {
            Steps = steps,
            StepAttachmentNames = attachmentNames,
        };
    }

    private async Task<List<Step>> ConvertListStepsForTestCase(List<TestRailStep> testRailSteps, Guid id, Dictionary<int, SharedStep> sharedStepMap)
    {
        var steps = new List<Step>();

        for (var i = 0; i < testRailSteps.Count; i++)
        {
            var testRailStep = testRailSteps[i];

            if (testRailStep.SharedStepId != null)
            {
                var sharedStep = sharedStepMap[(int)testRailStep.SharedStepId];

                steps.Add(
                    new Step
                    {
                        SharedStepId = sharedStep.Id
                    }
                );

                i += sharedStep.Steps.Count - 1;

                continue;
            }

            var step = await ConvertStep(testRailStep.Action, testRailStep.Expected, id);

            steps.Add(step);
        }

        return steps;
    }

    private async Task<List<Step>> ConvertScenariosForTestCase(List<TestRailScenario> testRailScenarios, Guid id)
    {
        var steps = new List<Step>();

        foreach (var testRailScenario in testRailScenarios)
        {
            var step = await ConvertStep(testRailScenario.Action, string.Empty, id);

            steps.Add(step);
        }

        return steps;
    }

    private async Task<Step> ConvertStep(string? textAction, string? textExpected, Guid id)
    {
        var actionData = await ExtractAttachments(textAction, id);
        var expectedData = await ExtractAttachments(textExpected, id);

        return new Step
        {
            Action = ConvertTabels(ConvertingHyperlinks(actionData.Description)),
            Expected = ConvertTabels(ConvertingHyperlinks(expectedData.Description)),
            ActionAttachments = actionData.AttachmentNames,
            ExpectedAttachments = expectedData.AttachmentNames,
        };
    }

    private async Task<DescriptionInfo> ExtractAttachments(string? description, Guid id)
    {
        if (string.IsNullOrEmpty(description))
        {
            return new DescriptionInfo
            {
                Description = string.Empty,
                AttachmentNames = new List<string>()
            };
        }

        var info = new DescriptionInfo
        {
            Description = description,
            AttachmentNames = new List<string>()
        };

        var matches = _ImgRegex.Matches(description);

        if (matches.Count == 0)
        {
            return info;
        }

        foreach (Match match in matches)
        {
            var url = match.Groups[1].Value;
            var attachmentId = url.Split('/').Last();
            var fileName = string.Empty;

            if (!_attachmentsInfo.AttachmentsMap.TryGetValue(attachmentId, out fileName))
            {
                fileName = await GetAttachmentName(attachmentId, id);
            }

            if (fileName == string.Empty)
            {
                logger.LogInformation("There is no way to download attachment by id {AttachmentId}", attachmentId);

                continue;
            }

            info.Description = info.Description.Replace(match.Value, $"<<<{fileName}>>>");
            info.AttachmentNames.Add(fileName);
        }

        return info;
    }

    private async Task<string> GetAttachmentName(string attachmentId, Guid id)
    {
        if (!int.TryParse(attachmentId, out int value))
        {
            return string.Empty;
        }

        var fileName = await attachmentService.DownloadAttachmentById(value, id);

        _attachmentsInfo.AttachmentNames.Add(fileName);
        _attachmentsInfo.AttachmentsMap.Add(attachmentId, fileName);

        return fileName;
    }

    public static string ConvertingHyperlinks(string description)
    {
        var matches = _HyperlinkRegex.Matches(description);

        if (matches.Count == 0)
        {
            return description;
        }

        foreach (Match match in matches)
        {
            var urlMatch = _UrlRegex.Match(match.Value);

            if (!urlMatch.Success) continue;

            var url = urlMatch.Groups[1].Value;
            var titleMatch = _TitleRegex.Match(match.Value);
            var title = titleMatch.Success ? titleMatch.Groups[1].Value : url;
            description = description.Replace(match.Value, $"<a target=\"_blank\" rel=\"noopener noreferrer\" href=\"{url}\">{title}</a>");
        }
        return description;
    }

    private static string ConvertTabels(string description)
    {
        var tableMatches = _TableRegex.Matches(description);

        if (tableMatches.Count == 0)
        {
            return description;
        }

        foreach (Match tableMatch in tableMatches)
        {
            var table = tableMatch.Value;

            var convertedLines = ConvertLines(table);
            var convertedTable = "<table style=\"min-width: 165px\"><tbody>" + convertedLines + "</tbody></table>";

            description = description.Replace(table, convertedTable);
        }
        return description;
    }

    private static string ConvertLines(string table)
    {
        var lines = table.Split('\n');
        var convertedLines = "";

        foreach (var line in lines)
        {
            var convertedCells = ConvertCells(line);
            var convertedLine = "<tr>" + convertedCells + "</tr>";

            convertedLines += convertedLine;
        }

        return convertedLines;
    }

    private static string ConvertCells(string line)
    {
        var cellMatches = _CellRegex.Matches(line);

        if (cellMatches.Count == 0)
        {
            return line;
        }

        var cells = "";

        foreach (Match cellMatche in cellMatches)
        {
            var value = cellMatche.Groups[1].Value;
            var cell = $"<td colspan=\"1\" rowspan=\"1\"><p class=\"tiptap-text\" style=\"text-align: left\">{value}</p></td>";

            cells += cell;
        }

        return cells;
    }
}
