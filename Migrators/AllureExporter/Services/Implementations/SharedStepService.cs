using AllureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;
using Attribute = Models.Attribute;

namespace AllureExporter.Services.Implementations;

internal class SharedStepService(
    ILogger<SharedStepService> logger,
    IClient client,
    IStepService stepService,
    IAttachmentService attachmentService)
    : ISharedStepService
{
    public async Task<Dictionary<long, SharedStep>> ConvertSharedSteps(
        long projectId,
        Guid sectionId,
        List<Attribute> attributes)
    {
        logger.LogInformation("Converting shared steps");

        var sharedSteps = await client.GetSharedStepsByProjectId(projectId);

        logger.LogDebug("Found {Count} shared steps: {@SharedSteps}", sharedSteps.Count, sharedSteps);

        var tmsSharedSteps = new Dictionary<long, SharedStep>();

        foreach (var sharedStep in sharedSteps)
        {
            var sharedStepInfo = await client.GetStepsInfoBySharedStepId(sharedStep.Id);

            logger.LogDebug("Found shared step info by id {SharedStepId}: {@sharedStepInfo}", sharedStep.Id,
                sharedStepInfo);

            var steps = await stepService.ConvertStepsForSharedStep(sharedStep.Id);

            logger.LogDebug("Found {@Steps} steps", steps.Count);

            var sharedStepGuid = Guid.NewGuid();
            var tmsAttachments =
                await attachmentService.DownloadAttachmentsforSharedStep(sharedStep.Id, sharedStepGuid);

            var step = new SharedStep
            {
                Id = sharedStepGuid,
                Name = sharedStep.Name,
                Description = string.Empty,
                Steps = steps,
                State = StateType.NotReady,
                Priority = PriorityType.Medium,
                Attributes = attributes.Select(a =>
                        new CaseAttribute
                        {
                            Id = a.Id,
                            Value = string.Empty
                        })
                    .ToList(),
                Links = new List<Link>(),
                Tags = new List<string>(),
                Attachments = tmsAttachments,
                SectionId = sectionId
            };

            logger.LogDebug("Converted shared step: {@Step}", step);

            tmsSharedSteps.Add(sharedStep.Id, step);
        }

        return tmsSharedSteps;
    }
}
