using Microsoft.Extensions.Logging;
using Models;
using Moq;
using NUnit.Framework;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Attributes;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrAttributeType = ZephyrScaleServerExporter.Models.Attributes.ZephyrAttributeType;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Implementations;
using ZephyrScaleServerExporterTests.Helpers;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class AttributeServiceTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<ILogger<AttributeService>> _mockLogger;
    private Mock<IClient> _mockClient;
    private AttributeService _attributeService;
    private string _projectId;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockLogger = new Mock<ILogger<AttributeService>>();
        _mockClient = new Mock<IClient>();
        
        _attributeService = new AttributeService(
            _mockDetailedLogService.Object,
            _mockLogger.Object,
            _mockClient.Object);
        
        _projectId = TestDataHelper.GenerateProjectId().ToString();
    }

    #region ConvertAttributes

    [Test]
    public async Task ConvertAttributes_WithComponentsAndCustomFields_ReturnsCorrectAttributeData()
    {
        // Arrange
        var projectIdInt = int.Parse(_projectId);
        var baseFieldId = TestDataHelper.GenerateProjectId();
        
        var components = new List<JiraComponent>
        {
            new() { Name = "Backend" },
            new() { Name = "Frontend" },
            new() { Name = "Mobile" }
        };

        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Id = baseFieldId,
                Name = "Priority",
                Type = ZephyrAttributeType.Options,
                Required = true,
                ProjectId = projectIdInt,
                Index = 0,
                Archived = false,
                Options = new List<ZephyrCustomFieldOption>
                {
                    new() { Id = TestDataHelper.GenerateProjectId(1, 1000), Name = "Critical" },
                    new() { Id = TestDataHelper.GenerateProjectId(1, 1000), Name = "High" },
                    new() { Id = TestDataHelper.GenerateProjectId(1, 1000), Name = "Medium" },
                    new() { Id = TestDataHelper.GenerateProjectId(1, 1000), Name = "Low" }
                }
            },
            new()
            {
                Id = baseFieldId + 1,
                Name = "Test Environment",
                Type = "TEXT",
                Required = false,
                ProjectId = projectIdInt,
                Index = 1,
                Archived = false,
                Options = null
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 4);

            AssertAttribute(
                result,
                attributeName: Constants.ComponentAttribute,
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 3,
                "Backend", "Frontend", "Mobile");

            AssertAttribute(
                result,
                attributeName: Constants.IdZephyrAttribute,
                expectedType: AttributeType.String,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttribute(
                result,
                attributeName: "Priority",
                expectedType: AttributeType.Options,
                expectedIsRequired: true,
                expectedIsActive: true,
                expectedOptionsCount: 4,
                "Critical", "High", "Medium", "Low");

            AssertAttribute(
                result,
                attributeName: "Test Environment",
                expectedType: AttributeType.String,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttributeMapKeys(result, Constants.ComponentAttribute, Constants.IdZephyrAttribute, "Priority", "Test Environment");

            var ids = result.Attributes.Select(a => a.Id).ToList();
            Assert.That(ids, Has.Count.EqualTo(ids.Distinct().Count()));
        });

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting attributes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockDetailedLogService.Verify(
            x => x.LogDebug("Attributes: {@Attribute}", It.IsAny<object[]>()),
            Times.Once);
    }

    [Test]
    public async Task ConvertAttributes_WithEmptyComponentsAndCustomFields_ReturnsAttributeDataWithEmptyComponentOptions()
    {
        // Arrange
        var components = new List<JiraComponent>();
        var customFields = new List<ZephyrCustomFieldForTestCase>();

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 2);

            AssertAttribute(
                result,
                attributeName: Constants.ComponentAttribute,
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttribute(
                result,
                attributeName: Constants.IdZephyrAttribute,
                expectedType: AttributeType.String,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttributeMapKeys(result, Constants.ComponentAttribute, Constants.IdZephyrAttribute);
        });
    }

    [Test]
    public async Task ConvertAttributes_WithEmptyOnlyCustomFields_ReturnsOnlySystemAttributes()
    {
        // Arrange
        var components = new List<JiraComponent>
        {
            new() { Name = "Component1" }
        };
        var customFields = new List<ZephyrCustomFieldForTestCase>();

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 2);

            AssertAttribute(
                result,
                attributeName: Constants.ComponentAttribute,
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 1,
                "Component1");

            AssertAttribute(
                result,
                attributeName: Constants.IdZephyrAttribute,
                expectedType: AttributeType.String,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttributeMapKeys(result, Constants.ComponentAttribute, Constants.IdZephyrAttribute);
        });
    }

    [Test]
    public async Task ConvertAttributes_WithAllAttributeTypes_ConvertsAllTypesCorrectly()
    {
        // Arrange
        var components = new List<JiraComponent>();
        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Name = "OptionsField",
                Type = ZephyrAttributeType.Options,
                Required = false,
                Options = new List<ZephyrCustomFieldOption>
                {
                    new() { Id = 1, Name = "Option1" }
                }
            },
            new()
            {
                Name = "MultipleOptionsField",
                Type = ZephyrAttributeType.MultipleOptions,
                Required = false,
                Options = new List<ZephyrCustomFieldOption>
                {
                    new() { Id = 2, Name = "Option2" }
                }
            },
            new()
            {
                Name = "DatetimeField",
                Type = ZephyrAttributeType.Datetime,
                Required = false,
                Options = null
            },
            new()
            {
                Name = "CheckboxField",
                Type = ZephyrAttributeType.Checkbox,
                Required = false,
                Options = null
            },
            new()
            {
                Name = "StringField",
                Type = "UNKNOWN_TYPE",
                Required = false,
                Options = null
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 7);

            AssertAttribute(
                result,
                attributeName: "OptionsField",
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 1,
                "Option1");

            AssertAttribute(
                result,
                attributeName: "MultipleOptionsField",
                expectedType: AttributeType.MultipleOptions,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 1,
                "Option2");

            AssertAttribute(
                result,
                attributeName: "DatetimeField",
                expectedType: AttributeType.Datetime,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttribute(
                result,
                attributeName: "CheckboxField",
                expectedType: AttributeType.Checkbox,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttribute(
                result,
                attributeName: "StringField",
                expectedType: AttributeType.String,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttributeMapKeys(result, "OptionsField", "MultipleOptionsField", "DatetimeField", "CheckboxField", "StringField");
        });
    }

    #endregion

    #region ConvertOptions

    [Test]
    public async Task ConvertAttributes_StringField_WithOptions_TreatsAsStringAndIgnoresOptions()
    {
        // Arrange
        var components = new List<JiraComponent>();
        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Name = "StringFieldWithOptions",
                Type = "TEXT",
                Required = false,
                Options = new List<ZephyrCustomFieldOption>
                {
                    new() { Id = 1, Name = "OptionA" },
                    new() { Id = 2, Name = "OptionB" }
                }
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 3);

            AssertAttribute(
                result,
                attributeName: "StringFieldWithOptions",
                expectedType: AttributeType.String,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 0);

            AssertAttributeMapKeys(result, "StringFieldWithOptions");
        });
    }

    [Test]
    public async Task ConvertAttributes_WithDuplicateOptionNames_DeduplicatesOptions()
    {
        // Arrange
        var components = new List<JiraComponent>();
        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Name = "TestField",
                Type = ZephyrAttributeType.Options,
                Required = false,
                Options = new List<ZephyrCustomFieldOption>
                {
                    new() { Id = 1, Name = "DuplicateOption" },
                    new() { Id = 2, Name = "UniqueOption" },
                    new() { Id = 3, Name = "DuplicateOption" },
                    new() { Id = 4, Name = "AnotherUnique" }
                }
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 3);

            AssertAttribute(
                result,
                attributeName: "TestField",
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 3,
                "DuplicateOption", "UniqueOption", "AnotherUnique");

            AssertAttributeMapKeys(result, "TestField");
        });

        _mockDetailedLogService.Verify(
            x => x.LogDebug("The option \"{Option}\" has already been added to the attribute", It.Is<object[]>(o => o[0]!.ToString() == "DuplicateOption")),
            Times.Once);
    }

    [Test]
    public async Task ConvertAttributes_WithEmptyOptionsList_ReturnsEmptyOptions()
    {
        // Arrange
        var components = new List<JiraComponent>();
        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Name = "EmptyOptionsField",
                Type = ZephyrAttributeType.Options,
                Required = false,
                Options = new List<ZephyrCustomFieldOption>()
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 3);

            AssertAttribute(
                result,
                attributeName: "EmptyOptionsField",
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttributeMapKeys(result, "EmptyOptionsField");
        });
    }

    #endregion

    #region ConvertAttributes - Exception Handling

    [Test]
    [TestCase("Failed to get components", true, false)]
    [TestCase("Failed to get custom fields", false, true)]
    public void ConvertAttributes_ClientThrowsException_PropagatesException(
        string errorMessage,
        bool componentsThrows,
        bool customFieldsThrows)
    {
        // Arrange
        var exception = new Exception(errorMessage);
        var components = new List<JiraComponent>();

        if (componentsThrows)
        {
            _mockClient.Setup(x => x.GetComponents()).ThrowsAsync(exception);
        }
        else
        {
            _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
            _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ThrowsAsync(exception);
        }

        // Act & Assert
        Assert.That(
            async () => await _attributeService.ConvertAttributes(_projectId),
            Throws.Exception.EqualTo(exception));
    }

    [Test]
    public async Task ConvertAttributes_WithCustomFieldOptionsNull_SkipsOptionsTypeAttribute()
    {
        // Arrange
        var components = new List<JiraComponent>();
        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Name = "OptionsFieldWithoutOptions",
                Type = ZephyrAttributeType.Options,
                Required = false,
                Options = null
            },
            new()
            {
                Name = "MultipleOptionsFieldWithoutOptions",
                Type = ZephyrAttributeType.MultipleOptions,
                Required = false,
                Options = null
            },
            new()
            {
                Name = "StringField",
                Type = "TEXT",
                Required = false,
                Options = null
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 3);
            Assert.That(result.Attributes.Any(a => a.Name == "OptionsFieldWithoutOptions"), Is.False);
            Assert.That(result.Attributes.Any(a => a.Name == "MultipleOptionsFieldWithoutOptions"), Is.False);
            Assert.That(result.Attributes.Any(a => a.Name == "StringField"), Is.True);

            AssertAttributeMapKeys(result, "StringField");
            AssertAttributeMapDoesNotContainKeys(result, "OptionsFieldWithoutOptions", "MultipleOptionsFieldWithoutOptions");
        });

        _mockDetailedLogService.Verify(
            x => x.LogDebug("The attribute \"{Name}\" with {Type} type without options", It.Is<object[]>(o => o[0]!.ToString() == "OptionsFieldWithoutOptions" && o[1]!.ToString() == ZephyrAttributeType.Options)),
            Times.Once);

        _mockDetailedLogService.Verify(
            x => x.LogDebug("The attribute \"{Name}\" with {Type} type without options", It.Is<object[]>(o => o[0]!.ToString() == "MultipleOptionsFieldWithoutOptions" && o[1]!.ToString() == ZephyrAttributeType.MultipleOptions)),
            Times.Once);
    }

    [Test]
    [TestCase("RequiredField", true)]
    [TestCase("OptionalField", false)]
    public async Task ConvertAttributes_WithCustomFieldRequiredFlag_ConvertsCorrectly(
        string fieldName,
        bool isRequired)
    {
        // Arrange
        var components = new List<JiraComponent>();
        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Name = fieldName,
                Type = "TEXT",
                Required = isRequired,
                Options = null
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 3);

            AssertAttribute(
                result,
                attributeName: fieldName,
                expectedType: AttributeType.String,
                expectedIsRequired: isRequired,
                expectedIsActive: true);

            AssertAttributeMapKeys(result, fieldName);
        });
    }

    [Test]
    [TestCase("StringField", "TEXT", AttributeType.String)]
    [TestCase("DatetimeField", ZephyrAttributeType.Datetime, AttributeType.Datetime)]
    [TestCase("CheckboxField", ZephyrAttributeType.Checkbox, AttributeType.Checkbox)]
    [TestCase("UnknownTypeField", "UNKNOWN_TYPE_12345", AttributeType.String)]
    public async Task ConvertAttributes_WithCustomFieldType_ConvertsToCorrectType(
        string fieldName,
        string zephyrType,
        AttributeType expectedType)
    {
        // Arrange
        var components = new List<JiraComponent>();
        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Name = fieldName,
                Type = zephyrType,
                Required = false,
                Options = null
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 3);

            AssertAttribute(
                result,
                attributeName: fieldName,
                expectedType: expectedType,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttributeMapKeys(result, fieldName);
        });
    }

    [Test]
    public async Task ConvertAttributes_WithSpecialCharactersInNames_HandlesCorrectly()
    {
        // Arrange
        var components = new List<JiraComponent>
        {
            new() { Name = "Component/With-Special_Chars" }
        };
        var customFields = new List<ZephyrCustomFieldForTestCase>
        {
            new()
            {
                Name = "Field@With#Special$Chars%",
                Type = "TEXT",
                Required = false,
                Options = null
            },
            new()
            {
                Name = " ",
                Type = ZephyrAttributeType.Options,
                Required = false,
                Options = new List<ZephyrCustomFieldOption>
                {
                    new() { Id = 1, Name = " " }
                }
            }
        };

        _mockClient.Setup(x => x.GetComponents()).ReturnsAsync(components);
        _mockClient.Setup(x => x.GetCustomFieldsForTestCases(_projectId)).ReturnsAsync(customFields);

        // Act
        var result = await _attributeService.ConvertAttributes(_projectId);

        // Assert
        Assert.Multiple(() =>
        {
            AssertAttributeResult(result, 4);

            AssertAttribute(
                result,
                attributeName: Constants.ComponentAttribute,
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 1,
                "Component/With-Special_Chars");

            AssertAttribute(
                result,
                attributeName: "Field@With#Special$Chars%",
                expectedType: AttributeType.String,
                expectedIsRequired: false,
                expectedIsActive: true);

            AssertAttribute(
                result,
                attributeName: " ",
                expectedType: AttributeType.Options,
                expectedIsRequired: false,
                expectedIsActive: true,
                expectedOptionsCount: 1,
                " ");

            AssertAttributeMapKeys(result, Constants.ComponentAttribute, "Field@With#Special$Chars%", " ");
        });
    }

    #endregion

    #region Assert Helpers

    private static void AssertAttributeResult(AttributeData? result, int expectedCount)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Attributes, Is.Not.Null);
            Assert.That(result.AttributeMap, Is.Not.Null);
            Assert.That(result.Attributes, Has.Count.EqualTo(expectedCount));
            Assert.That(result.AttributeMap, Has.Count.EqualTo(expectedCount));
        });
    }

    private static void AssertAttribute(
        AttributeData? result,
        string attributeName,
        AttributeType expectedType,
        bool expectedIsRequired,
        bool expectedIsActive,
        int expectedOptionsCount = 0,
        params string[] expectedOptions)
    {
        var attribute = result!.Attributes.FirstOrDefault(a => a.Name == attributeName);

        Assert.Multiple(() =>
        {
            Assert.That(attribute, Is.Not.Null, $"{attributeName} attribute should exist");
            Assert.That(attribute!.Type, Is.EqualTo(expectedType));
            Assert.That(attribute.IsRequired, Is.EqualTo(expectedIsRequired));
            Assert.That(attribute.IsActive, Is.EqualTo(expectedIsActive));
            Assert.That(attribute.Options, Has.Count.EqualTo(expectedOptionsCount));
        });

        foreach (var option in expectedOptions)
        {
            Assert.That(attribute.Options, Contains.Item(option));
        }
    }

    private static void AssertAttributeMapKeys(AttributeData? result, params string[] expectedKeys)
    {
        foreach (var key in expectedKeys)
        {
            Assert.That(result!.AttributeMap.ContainsKey(key), Is.True, $"AttributeMap should contain key '{key}'");
        }
    }

    private static void AssertAttributeMapDoesNotContainKeys(AttributeData? result, params string[] keys)
    {
        foreach (var key in keys)
        {
            Assert.That(result!.AttributeMap.ContainsKey(key), Is.False, $"AttributeMap should not contain key '{key}'");
        }
    }

    #endregion
}
