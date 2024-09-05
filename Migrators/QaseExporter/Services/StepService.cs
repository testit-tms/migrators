using QaseExporter.Models;
using Microsoft.Extensions.Logging;
using Models;
using System.Collections.Generic;

namespace QaseExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IAttachmentService _attachmentService;

    public StepService(ILogger<StepService> logger, IAttachmentService attachmentService)
    {
        _logger = logger;
        _attachmentService = attachmentService;
    }

    public async Task<List<Step>> ConvertSteps(List<QaseStep> qaseSteps, Dictionary<string, SharedStep> sharedSteps, Guid testCaseId)
    {
        _logger.LogDebug("Found steps: {@Steps}", qaseSteps);

        var steps = new List<Step>();

        for (int i = 0; i < qaseSteps.Count; i++)
        {
            var qaseStep = qaseSteps[i];

            if (qaseStep.SharedStepHash != null)
            {
                steps.Add(
                    new Step
                    {
                        Action = string.Empty,
                        Expected = string.Empty,
                        ActionAttachments = new List<string>(),
                        ExpectedAttachments = new List<string>(),
                        TestDataAttachments = new List<string>(),
                        SharedStepId = sharedSteps[qaseStep.SharedStepHash].Id,
                    }
                );

                i += sharedSteps[qaseStep.SharedStepHash].Steps.Count - 1;

                continue;
            }

            var step = await ConvertStep(qaseStep, testCaseId);

            steps.Add(step);

            if (qaseStep.Steps != null && qaseStep.Steps.Any())
            {
                var childSteps = await ConvertSteps(qaseStep.Steps, sharedSteps, testCaseId);

                steps.AddRange(childSteps);
            }
        }
        _logger.LogDebug("Converted steps: {@Steps}", steps);

        return steps;
    }

    public async Task<List<Step>> ConvertConditionSteps(string conditions, Guid testCaseId)
    {
        var action = Utils.ExtractAttachments(conditions);

        var newStep = new Step
        {
            Action = action.Description,
            Expected = string.Empty,
            TestData = string.Empty,
            ActionAttachments = new List<string>(),
            ExpectedAttachments = new List<string>(),
            TestDataAttachments = new List<string>()
        };

        if (action.Attachments.Count > 0)
        {
            foreach (var attachment in action.Attachments)
            {
                var fileNames = await _attachmentService.DownloadAttachments(new List<QaseAttachment>() { attachment }, testCaseId);
                newStep.ActionAttachments.AddRange(fileNames);
            }
        }

        return new List<Step>() { newStep };
    }

    private async Task<Step> ConvertStep(QaseStep step, Guid testCaseId)
    {
        var action = Utils.ExtractAttachments(step.Action);
        var expected = Utils.ExtractAttachments(step.ExpectedResult);
        var testData = Utils.ExtractAttachments(step.Data);

        var newStep = new Step
        {
            Action = action.Description,
            Expected = expected.Description,
            TestData = testData.Description,
            ActionAttachments = new List<string>(),
            ExpectedAttachments = new List<string>(),
            TestDataAttachments = new List<string>()
        };

        if (action.Attachments.Count > 0)
        {
            foreach (var attachment in action.Attachments)
            {
                var fileNames = await _attachmentService.DownloadAttachments(new List<QaseAttachment>() { attachment }, testCaseId);
                newStep.ActionAttachments.AddRange(fileNames);
            }
        }

        if (expected.Attachments.Count > 0)
        {
            foreach (var attachment in expected.Attachments)
            {
                var fileNames = await _attachmentService.DownloadAttachments(new List<QaseAttachment>() { attachment }, testCaseId);
                newStep.ExpectedAttachments.AddRange(fileNames);
            }
        }

        if (testData.Attachments.Count > 0)
        {
            foreach (var attachment in testData.Attachments)
            {
                var fileNames = await _attachmentService.DownloadAttachments(new List<QaseAttachment>() { attachment }, testCaseId);
                newStep.TestDataAttachments.AddRange(fileNames);
            }
        }

        return newStep;
    }
}
