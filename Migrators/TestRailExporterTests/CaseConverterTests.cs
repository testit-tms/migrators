using System.Text.Json;
using Models;
using TestRailExporter.Models.Client;
using TestRailExporter.Models.Commons;
using TestRailExporter.Services.Implementations;
using Attribute = Models.Attribute;

namespace TestRailExporterTests;

public class CaseConverterTests
{
    [TestCase(4, "Critical", PriorityType.Highest)]
    [TestCase(3, "High", PriorityType.High)]
    [TestCase(2, "Medium", PriorityType.Medium)]
    [TestCase(1, "Low", PriorityType.Low)]
    [TestCase(1, "Lowest", PriorityType.Lowest)]
    [TestCase(99, "Custom", PriorityType.Medium)]
    public void ConvertPriority_UsesNameFromDictionary(int id, string name, PriorityType expected)
    {
        var names = new Dictionary<int, string> { [id] = name };

        Assert.That(CaseConverter.ConvertPriority(id, names), Is.EqualTo(expected));
    }

    [TestCase(1, PriorityType.Low)]
    [TestCase(2, PriorityType.Medium)]
    [TestCase(3, PriorityType.High)]
    [TestCase(4, PriorityType.Highest)]
    [TestCase(99, PriorityType.Medium)]
    public void ConvertPriority_FallsBackToId(int id, PriorityType expected)
    {
        Assert.That(CaseConverter.ConvertPriority(id, new Dictionary<int, string>()), Is.EqualTo(expected));
    }

    [Test]
    public void ConvertState_UsesNativeStatusId()
    {
        var data = new AttributeData { StatusNames = { [2] = "Approved" } };
        var testCase = new TestRailCase { StatusId = 2 };

        Assert.That(CaseConverter.ConvertState(testCase, data), Is.EqualTo(StateType.Ready));
    }

    [Test]
    public void ConvertState_UsesTcStateWhenStatusIdMissing()
    {
        var field = Dropdown("custom_tc_state", "tc_state", "1, Draft\n2, Ready");
        var attribute = new Attribute { Id = Guid.NewGuid(), Name = "tc_state" };
        var data = CreateData(field, attribute);
        var testCase = new TestRailCase
        {
            CustomFields = { ["custom_tc_state"] = Json("2") }
        };

        Assert.That(CaseConverter.ConvertState(testCase, data), Is.EqualTo(StateType.Ready));
    }

    [Test]
    public void ConvertState_NeedsWork_WhenUnknown()
    {
        var testCase = new TestRailCase();

        Assert.That(CaseConverter.ConvertState(testCase, new AttributeData()), Is.EqualTo(StateType.NeedsWork));
    }

    [TestCase("Needs Work", StateType.NeedsWork)]
    [TestCase("In Review", StateType.NeedsWork)]
    [TestCase("Not Ready", StateType.NotReady)]
    [TestCase("Draft", StateType.NotReady)]
    [TestCase("Ready", StateType.Ready)]
    [TestCase("Unknown", StateType.NeedsWork)]
    public void MapStateName_MatchesKeywords(string name, StateType expected)
    {
        Assert.That(CaseConverter.MapStateName(name), Is.EqualTo(expected));
    }

    [Test]
    public void ConvertAttributes_ResolvesDropdownIdToLabel()
    {
        var field = Dropdown("custom_tc_state", "tc_state", "1, Draft\n2, Ready");
        var attribute = new Attribute { Id = Guid.NewGuid(), Name = "tc_state" };
        var data = CreateData(field, attribute);
        var testCase = new TestRailCase
        {
            CustomFields = { ["custom_tc_state"] = Json("2") }
        };

        var attributes = CaseConverter.ConvertAttributes(testCase, data);

        Assert.That(attributes, Has.Count.EqualTo(1));
        Assert.That(attributes[0].Id, Is.EqualTo(attribute.Id));
        Assert.That(attributes[0].Value, Is.EqualTo("Ready"));
    }

    [Test]
    public void ConvertAttributes_SkipsEmptyValues()
    {
        var field = Dropdown("custom_tc_state", "tc_state", "1, Draft\n2, Ready");
        var attribute = new Attribute { Id = Guid.NewGuid(), Name = "tc_state" };
        var data = CreateData(field, attribute);
        var testCase = new TestRailCase
        {
            CustomFields = { ["custom_tc_state"] = Json("null") }
        };

        Assert.That(CaseConverter.ConvertAttributes(testCase, data), Is.Empty);
    }

    [Test]
    public void ConvertAttributes_SkipsUnmatchedDropdownId()
    {
        var field = Dropdown("custom_tc_state", "tc_state", "1, Draft\n2, Ready");
        var attribute = new Attribute { Id = Guid.NewGuid(), Name = "tc_state" };
        var data = CreateData(field, attribute);
        var testCase = new TestRailCase
        {
            CustomFields = { ["custom_tc_state"] = Json("9") }
        };

        Assert.That(CaseConverter.ConvertAttributes(testCase, data), Is.Empty);
    }

    [Test]
    public void ResolveValue_KeepsStringLabel()
    {
        var field = Dropdown("custom_type", "type", "1, Functional\n2, Other");

        Assert.That(CaseConverter.ResolveValue(Json("\"Functional\""), field), Is.EqualTo("Functional"));
    }

    private static AttributeData CreateData(TestRailCaseField field, Attribute attribute) => new()
    {
        Attributes = [attribute],
        AttributesBySystemName = { [field.SystemName] = attribute },
        FieldsBySystemName = { [field.SystemName] = field }
    };

    private static TestRailCaseField Dropdown(string systemName, string name, string items) => new()
    {
        SystemName = systemName,
        Name = name,
        Label = name,
        TypeId = TestRailCaseField.TypeDropdown,
        IsActive = true,
        Configs =
        [
            new TestRailCaseFieldConfig
            {
                Options = new TestRailCaseFieldOptions { Items = items }
            }
        ]
    };

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
