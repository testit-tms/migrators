using System.Text.Json;
using Models;
using TestRailExporter.Models.Client;
using TestRailExporter.Models.Commons;

namespace TestRailExporter.Services.Implementations;

public static class CaseConverter
{
    private static readonly HashSet<string> SkippedSystemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "custom_preconds",
        "custom_steps",
        "custom_expected",
        "custom_mission",
        "custom_goals",
        "custom_testrail_bdd_scenario",
        "custom_steps_separated"
    };

    public static bool IsSkippedField(TestRailCaseField field) =>
        !field.IsActive
        || field.TypeId is TestRailCaseField.TypeSteps or TestRailCaseField.TypeStepResults
        || SkippedSystemNames.Contains(field.SystemName);

    public static AttributeType ConvertType(int typeId) => typeId switch
    {
        TestRailCaseField.TypeCheckbox => AttributeType.Checkbox,
        TestRailCaseField.TypeDropdown => AttributeType.Options,
        TestRailCaseField.TypeMultiSelect => AttributeType.MultipleOptions,
        TestRailCaseField.TypeDate => AttributeType.Datetime,
        _ => AttributeType.String
    };

    public static List<string> ParseOptionLabels(TestRailCaseField field) =>
        ParseOptions(field).Values.Distinct(StringComparer.Ordinal).ToList();

    public static PriorityType ConvertPriority(int priorityId, IReadOnlyDictionary<int, string> priorityNames)
    {
        if (priorityNames.TryGetValue(priorityId, out var name))
            return MapPriorityName(name);

        return priorityId switch
        {
            1 => PriorityType.Low,
            2 => PriorityType.Medium,
            3 => PriorityType.High,
            4 => PriorityType.Highest,
            _ => PriorityType.Medium
        };
    }

    public static PriorityType MapPriorityName(string? name)
    {
        var value = name?.Trim().ToLowerInvariant() ?? string.Empty;
        if (value.Contains("lowest")) return PriorityType.Lowest;
        if (value.Contains("highest") || value.Contains("critical")) return PriorityType.Highest;
        if (value.Contains("high")) return PriorityType.High;
        if (value.Contains("low")) return PriorityType.Low;
        return PriorityType.Medium;
    }

    public static StateType ConvertState(TestRailCase testRailCase, AttributeData attributeData)
    {
        if (testRailCase.StatusId is int statusId &&
            attributeData.StatusNames.TryGetValue(statusId, out var statusName))
            return MapStateName(statusName);

        var stateLabel = FindStateAttributeLabel(testRailCase, attributeData);
        return stateLabel != null ? MapStateName(stateLabel) : StateType.NeedsWork;
    }

    public static StateType MapStateName(string? name)
    {
        var value = name?.Trim().ToLowerInvariant() ?? string.Empty;
        if (value.Contains("rework") || value.Contains("needs work") || value.Contains("review"))
            return StateType.NeedsWork;
        if (value.Contains("not ready") || value.Contains("draft") || value.Contains("design"))
            return StateType.NotReady;
        if (value.Contains("ready") || value.Contains("approved") || value.Contains("active")
            || value.Contains("published") || value.Contains("completed"))
            return StateType.Ready;
        return StateType.NeedsWork;
    }

    public static List<CaseAttribute> ConvertAttributes(TestRailCase testRailCase, AttributeData attributeData)
    {
        var result = new List<CaseAttribute>();

        foreach (var (systemName, field) in attributeData.FieldsBySystemName)
        {
            if (!attributeData.AttributesBySystemName.TryGetValue(systemName, out var attribute))
                continue;
            if (!testRailCase.CustomFields.TryGetValue(systemName, out var raw))
                continue;

            var value = ResolveValue(raw, field);
            if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
                continue;

            result.Add(new CaseAttribute { Id = attribute.Id, Value = value });
        }

        return result;
    }

    public static object? ResolveValue(JsonElement value, TestRailCaseField field)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        var options = ParseOptions(field);

        return field.TypeId switch
        {
            TestRailCaseField.TypeCheckbox => ResolveCheckbox(value),
            TestRailCaseField.TypeDropdown => ResolveOption(ToRawString(value), options),
            TestRailCaseField.TypeMultiSelect => ResolveMultiOptions(value, options),
            TestRailCaseField.TypeDate => ResolveDate(value),
            _ => ToRawString(value)
        };
    }

    private static string? FindStateAttributeLabel(TestRailCase testRailCase, AttributeData attributeData)
    {
        foreach (var (systemName, field) in attributeData.FieldsBySystemName)
        {
            if (!IsStateField(field))
                continue;
            if (!testRailCase.CustomFields.TryGetValue(systemName, out var raw))
                continue;

            return ResolveValue(raw, field) as string;
        }

        return null;
    }

    private static bool IsStateField(TestRailCaseField field)
    {
        var name = field.Name ?? string.Empty;
        var systemName = field.SystemName ?? string.Empty;
        return name.Equals("tc_state", StringComparison.OrdinalIgnoreCase)
               || name.Equals("state", StringComparison.OrdinalIgnoreCase)
               || systemName.Equals("custom_tc_state", StringComparison.OrdinalIgnoreCase)
               || systemName.Equals("custom_state", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ResolveCheckbox(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return value.GetBoolean();
        if (value.ValueKind == JsonValueKind.Number)
            return value.TryGetInt32(out var n) && n != 0;

        return bool.TryParse(ToRawString(value), out var parsed) && parsed;
    }

    private static object? ResolveMultiOptions(JsonElement value, Dictionary<string, string> options)
    {
        var labels = new List<string>();

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                var label = ResolveOption(ToRawString(item), options);
                if (!string.IsNullOrEmpty(label))
                    labels.Add(label);
            }
        }
        else
        {
            foreach (var part in ToRawString(value)
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var label = ResolveOption(part, options);
                if (!string.IsNullOrEmpty(label))
                    labels.Add(label);
            }
        }

        return labels.Count == 0 ? null : labels;
    }

    private static string? ResolveOption(string raw, Dictionary<string, string> options)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (options.Count == 0)
            return raw;
        if (options.TryGetValue(raw, out var byId))
            return byId;
        if (options.Values.Contains(raw, StringComparer.Ordinal))
            return raw;

        return null;
    }

    private static string? ResolveDate(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unix) && unix > 0)
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime.ToString("O");

        var raw = ToRawString(value);
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    private static Dictionary<string, string> ParseOptions(TestRailCaseField field)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var items in field.Configs.Select(c => c.Options?.Items).Where(i => !string.IsNullOrWhiteSpace(i)))
        {
            foreach (var line in items!.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = line.IndexOf(',');
                if (separator <= 0)
                    continue;

                var id = line[..separator].Trim();
                var label = line[(separator + 1)..].Trim();
                if (id.Length > 0 && label.Length > 0)
                    options.TryAdd(id, label);
            }
        }

        return options;
    }

    private static string ToRawString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
        _ => value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? string.Empty : value.ToString()
    };
}
