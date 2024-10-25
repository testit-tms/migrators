using Microsoft.Extensions.Logging;
using Models;
using QaseExporter.Client;
using QaseExporter.Models;
using System.Text.Json;

namespace QaseExporter.Services;

public class TestCaseService : ITestCaseService
{
    private readonly ILogger<TestCaseService> _logger;
    private readonly IClient _client;
    private readonly IStepService _stepService;
    private readonly IAttachmentService _attachmentService;
    private readonly IParameterService _parameterService;
    public const int _duration = 10000;
    public List<string> _systemAttributeNames = [
        QaseSystemFieldNames.AutomationStatus,
        QaseSystemFieldNames.Status,
        QaseSystemFieldNames.Priority,
        QaseSystemFieldNames.Type,
        QaseSystemFieldNames.Layer,
        QaseSystemFieldNames.IsFlaky,
        QaseSystemFieldNames.Severity,
        QaseSystemFieldNames.Behavior,
        QaseSystemFieldNames.ToBeAutomated];

    public TestCaseService(ILogger<TestCaseService> logger, IClient client, IStepService stepService,
        IAttachmentService attachmentService, IParameterService parameterService)
    {
        _logger = logger;
        _client = client;
        _stepService = stepService;
        _attachmentService = attachmentService;
        _parameterService = parameterService;
    }

    public async Task<List<TestCase>> ConvertTestCases(Dictionary<int, Guid> sectionMap, Dictionary<string, SharedStep> sharedSteps, AttributeData attributes)
    {
        _logger.LogInformation("Converting test cases");

        var testCases = new List<TestCase>();

        foreach (var section in sectionMap)
        {
            var qaseTestCases = await _client.GetTestCasesBySuiteId(section.Key);

            _logger.LogDebug("Found {Count} test cases", qaseTestCases.Count);

            testCases.AddRange(
                await ConvertTestCases(
                    qaseTestCases,
                    section.Value,
                    sharedSteps,
                    attributes
                )
            );
        }

        _logger.LogInformation("Exported test cases");

        return testCases;
    }

    private async Task<List<TestCase>> ConvertTestCases(List<QaseTestCase> qaseTestCases, Guid sectionId, Dictionary<string, SharedStep> sharedSteps, AttributeData attributes)
    {
        var testCases = new List<TestCase>();

        foreach (var qaseTestCase in qaseTestCases)
        {
            _logger.LogInformation("Converting test case {Name}", qaseTestCase.Name);

            var testCaseId = Guid.NewGuid();
            var attachments = await _attachmentService.DownloadAttachments(qaseTestCase.Attachments, testCaseId);
            var steps = await _stepService.ConvertSteps(qaseTestCase.Steps, sharedSteps, testCaseId);
            var preconditionSteps = await _stepService.ConvertConditionSteps(qaseTestCase.Preconditions, testCaseId);
            var postconditionSteps = await _stepService.ConvertConditionSteps(qaseTestCase.Postconditions, testCaseId);

            steps.ForEach(s =>
            {
                attachments.AddRange(s.ActionAttachments);
                attachments.AddRange(s.ExpectedAttachments);
                attachments.AddRange(s.TestDataAttachments);
            });
            preconditionSteps.ForEach(s =>
            {
                attachments.AddRange(s.ActionAttachments);
            });
            postconditionSteps.ForEach(s =>
            {
                attachments.AddRange(s.ActionAttachments);
            });

            var systemAttributes = ConvertSystemAttributes(attributes.SustemAttributeMap, qaseTestCase);
            var customAttributes = ConvertCustomAttributes(attributes.CustomAttributeMap, qaseTestCase.CustomFields);

            testCases.Add(
                new TestCase
                {
                    Id = testCaseId,
                    Description = ConvertingDescription(qaseTestCase.Description),
                    State = ConvertStatus(qaseTestCase.Status),
                    Priority = ConvertPriority(qaseTestCase.Priority),
                    Steps = steps,
                    PreconditionSteps = preconditionSteps,
                    PostconditionSteps = postconditionSteps,
                    Duration = _duration,
                    Attributes = systemAttributes.Concat(customAttributes).ToList(),
                    Tags = qaseTestCase.Tags.Select(t => t.Title).ToList(),
                    Attachments = attachments,
                    Iterations = qaseTestCase.Parameters.ToString() != "[]"
                        ? _parameterService.ConvertParameters(JsonSerializer.Deserialize<Dictionary<string, List<string>>>(qaseTestCase.Parameters.ToString()))
                        : new List<Iteration>(),
                    Links = new List<Link>(),
                    Name = qaseTestCase.Name,
                    SectionId = sectionId
                }
            );
        }

        return testCases;
    }

    private List<CaseAttribute> ConvertSystemAttributes(Dictionary<QaseSystemField, Guid> attributeMap, QaseTestCase qaseTestCase)
    {
        _logger.LogInformation("Converting system attributes for test case {Name}", qaseTestCase.Name);

        var attributes = new List<CaseAttribute>();

        foreach (var systemAttributeName in _systemAttributeNames)
        {
            var systemAttribute = attributeMap.Keys.FirstOrDefault(v => v.Title == systemAttributeName);

            if (systemAttribute == null)
            {
                _logger.LogError("Failed to getting \"{Name}\" attribute: {@Attributes}", systemAttributeName, attributeMap.Keys);

                continue;
            }

            if (systemAttributeName == QaseSystemFieldNames.ToBeAutomated)
            {
                attributes.Add(
                    new()
                    {
                        Id = attributeMap[systemAttribute],
                        Value = qaseTestCase.ToBeAutomated,
                    }
                );

                continue;
            }

            if (systemAttribute.Options == null)
            {
                _logger.LogError("Failed to converting option for \"{Name}\" attribute: {@Attribute}", systemAttributeName, systemAttribute);

                continue;
            }

            var optionId = GetOptionId(systemAttributeName, qaseTestCase);
            var value = systemAttribute.Options.FirstOrDefault(o => o.Id == optionId)?.Title;

            if (value == null)
            {
                _logger.LogError("Failed to getting option by id {Id} from the \"{Name}\" attribute: {@Options}", optionId, systemAttributeName, systemAttribute.Options);

                continue;
            }

            attributes.Add(
                new()
                {
                    Id = attributeMap[systemAttribute],
                    Value = value,
                }
            );
        }

        return attributes;
    }

    private int GetOptionId(string attributeName, QaseTestCase qaseTestCase)
    {
        switch (attributeName)
        {
            case QaseSystemFieldNames.AutomationStatus:
                return qaseTestCase.isManual ? 0 : 1;
            case QaseSystemFieldNames.Status:
                return qaseTestCase.Status;
            case QaseSystemFieldNames.Priority:
                return qaseTestCase.Priority;
            case QaseSystemFieldNames.Type:
                return qaseTestCase.Type;
            case QaseSystemFieldNames.Layer:
                return qaseTestCase.Layer;
            case QaseSystemFieldNames.IsFlaky:
                return qaseTestCase.IsFlaky;
            case QaseSystemFieldNames.Severity:
                return qaseTestCase.Severity;
            case QaseSystemFieldNames.Behavior:
                return qaseTestCase.Behavior;
            default:
                _logger.LogError("Problems getting the option id of the \"{Name}\" attribute", attributeName);

                return -1;
        }
    }

    //TODO: need to redo it easier
    private List<CaseAttribute> ConvertCustomAttributes(Dictionary<QaseCustomField, Guid> attributeMap, List<QaseCustomFieldValues> qaseCustomFieldValues)
    {
        _logger.LogInformation("Converting custom attributes: {@Attributes}", qaseCustomFieldValues);

        var attributes = new List<CaseAttribute>();
        var skippedQaseCustomFieldValues = new List<QaseCustomFieldValues>();

        foreach (var qaseCustomFieldValue in qaseCustomFieldValues)
        {
            if (skippedQaseCustomFieldValues.Contains(qaseCustomFieldValue))
            {
                continue;
            }

            var qaseCustomField = attributeMap.Keys.FirstOrDefault(f => f.Id.Equals(qaseCustomFieldValue.FieldId));

            if (qaseCustomField == null)
            {
                _logger.LogError("Custom field with id {Id} was not found in the attributeMap", qaseCustomFieldValue.FieldId);

                continue;
            }

            var attribute = new CaseAttribute
            {
                Id = attributeMap[qaseCustomField],
            };

            if (qaseCustomField.Type == QaseAttributeType.MultipleOptions)
            {
                if (qaseCustomField.Options == null)
                {
                    _logger.LogError("Custom field {Name} without options. Cannot convert option id {Value}", qaseCustomField.Title, qaseCustomFieldValue.Value);

                    continue;
                }

                var allQaseCustomFieldValues = qaseCustomFieldValues.Where(v => v.FieldId.Equals(qaseCustomField.Id));

                var value = new List<string>();

                foreach (var customFieldValue in allQaseCustomFieldValues)
                {
                    var qaseOption = qaseCustomField.Options.FirstOrDefault(o => o.Id.ToString().Equals(customFieldValue.Value));

                    value.Add(qaseCustomField.Options.FirstOrDefault(o => o.Id.ToString().Equals(customFieldValue.Value)).Title);
                }

                attribute.Value = value;

                skippedQaseCustomFieldValues.AddRange(allQaseCustomFieldValues);
            }
            else if (qaseCustomField.Type == QaseAttributeType.Options)
            {
                if (qaseCustomField.Options == null)
                {
                    _logger.LogError("Custom field {Name} without options. Cannot convert option id {Value}", qaseCustomField.Title, qaseCustomFieldValue.Value);

                    continue;
                }

                attribute.Value = qaseCustomField.Options.FirstOrDefault(o => o.Id.ToString().Equals(qaseCustomFieldValue.Value)).Title;
            }
            else if (qaseCustomField.Type == QaseAttributeType.Checkbox)
            {
                attribute.Value = true;
            }
            else if (qaseCustomField.Type == QaseAttributeType.Datetime)
            {
                attribute.Value = qaseCustomFieldValue.Value.Replace(" [^>]*", "");
            }
            else
            {
                attribute.Value = qaseCustomFieldValue.Value;
            }

            if (attribute.Value == null)
            {
                _logger.LogError("Problems converting the value {Value} for the custom field {Name}", qaseCustomFieldValue.Value, qaseCustomField.Title);

                continue;
            }

            attributes.Add(attribute);
        }
          
        return attributes;
    }

    private static PriorityType ConvertPriority(int priority)
    {
        return priority switch
        {
            1 => PriorityType.High,
            2 => PriorityType.Medium,
            3 => PriorityType.Low,
            _ => PriorityType.Medium
        };
    }

    private static StateType ConvertStatus(int status)
    {
        return status switch
        {
            0 => StateType.Ready,
            1 => StateType.NotReady,
            2 => StateType.NeedsWork,
            _ => StateType.NotReady
        };
    }

    private static string ConvertingDescription(string description)
    {
        return
            Utils.RemoveBackslashCharacters(
            Utils.RemoveToggleStrongCharacters(
            Utils.RemoveToggleStrikethroughCharacters(description)));
    }
}
