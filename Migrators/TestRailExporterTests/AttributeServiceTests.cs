using Microsoft.Extensions.Logging;
using NSubstitute;
using TestRailExporter.Client;
using TestRailExporter.Models.Client;
using TestRailExporter.Services.Implementations;

namespace TestRailExporterTests;

public class AttributeServiceTests
{
    [Test]
    public async Task ConvertAttributes_MapsCustomFieldsAndSkipsSteps()
    {
        var logger = Substitute.For<ILogger<AttributeService>>();
        var client = Substitute.For<IClient>();
        client.GetCaseFields().Returns(
        [
            new TestRailCaseField
            {
                SystemName = "custom_tc_state",
                Name = "tc_state",
                Label = "State",
                TypeId = TestRailCaseField.TypeDropdown,
                IsActive = true,
                Configs =
                [
                    new TestRailCaseFieldConfig
                    {
                        Options = new TestRailCaseFieldOptions { Items = "1, Draft\n2, Ready", IsRequired = false }
                    }
                ]
            },
            new TestRailCaseField
            {
                SystemName = "custom_steps_separated",
                Name = "steps_separated",
                Label = "Steps",
                TypeId = TestRailCaseField.TypeSteps,
                IsActive = true
            }
        ]);
        client.GetPriorities().Returns([new TestRailPriority { Id = 4, Name = "Critical" }]);
        client.GetCaseStatuses().Returns([new TestRailCaseStatus { Id = 1, Name = "Approved" }]);

        var result = await new AttributeService(logger, client).ConvertAttributes();

        Assert.That(result.Attributes, Has.Count.EqualTo(1));
        Assert.That(result.Attributes[0].Name, Is.EqualTo("State"));
        Assert.That(result.Attributes[0].Options, Is.EquivalentTo(new[] { "Draft", "Ready" }));
        Assert.That(result.PriorityNames[4], Is.EqualTo("Critical"));
        Assert.That(result.StatusNames[1], Is.EqualTo("Approved"));
        Assert.That(result.FieldsBySystemName.ContainsKey("custom_steps_separated"), Is.False);
    }
}
