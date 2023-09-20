using AzureExporter.Client;
using Microsoft.Extensions.Logging;
using Models;

namespace AzureExporter.Services;

public class SharedStepService : ISharedStepService
{
    private readonly ILogger<SharedStepService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;

    public SharedStepService(ILogger<SharedStepService> logger, IClient client, IStepService stepService, IAttachmentService attachmentService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
    }

    public async Task<Dictionary<int, SharedStep>> ConvertSharedSteps(Guid projectId, Guid sectionId)
    {
        var workItems = await _client.GetWorkItems(Constants.SharedStep);

        foreach (var workItem in workItems) {
            var sharedStep = await _client.GetWorkItemById(workItem.Id);

            _stepService.ReadTestCaseSteps(sharedStep);
        }

        return new Dictionary<int, SharedStep>();
    }
}
