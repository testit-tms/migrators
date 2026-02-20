using Microsoft.Extensions.Logging;
using Models;
using Moq;
using NUnit.Framework;
using System.Collections.Immutable;
using ZephyrScaleServerExporter.AttrubuteMapping;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.TestCase.Implementations;
using ZephyrScaleServerExporterTests.Helpers;
using Attribute = Models.Attribute;
using Constants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporterTests.Services.TestCase;

[TestFixture]
public class TestCaseAttributesServiceTests
{
    private Mock<IDetailedLogService> _mockDetailedLogService;
    private Mock<ILogger<TestCaseConvertService>> _mockLogger;
    private Mock<IMappingConfigReader> _mockMappingConfigReader;
    private TestCaseAttributesService _testCaseAttributesService;

    private Dictionary<string, Attribute> _attributeMap;
    private List<string> _requiredAttributeNames;

    [SetUp]
    public void SetUp()
    {
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockLogger = new Mock<ILogger<TestCaseConvertService>>();
        _mockMappingConfigReader = new Mock<IMappingConfigReader>();

        _testCaseAttributesService = new TestCaseAttributesService(
            _mockDetailedLogService.Object,
            _mockLogger.Object,
            _mockMappingConfigReader.Object);

        _attributeMap = TestDataHelper.CreateAttributeMap();
        _requiredAttributeNames = new List<string> { "RequiredAttribute" };
    }

    #region CalculateAttributes

    [Test]
    public void CalculateAttributes_WithFullData_ReturnsAllAttributes()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(
            component: "TestComponent",
            status: "Approved",
            customFields: new Dictionary<string, object>
            {
                { "CustomField1", "Value1" },
                { "CustomField2", "Value2" }
            });

        _attributeMap["CustomField1"] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = "CustomField1",
            Type = AttributeType.String,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        _attributeMap["CustomField2"] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = "CustomField2",
            Type = AttributeType.String,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        _requiredAttributeNames.Add("CustomField1");

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.GreaterThan(3));
            Assert.That(result.Any(a => a.Id == _attributeMap[Constants.ComponentAttribute].Id && (string)a.Value! == "TestComponent"), Is.True);
            Assert.That(result.Any(a => a.Id == _attributeMap[Constants.IdZephyrAttribute].Id && (string)a.Value! == zephyrTestCase.Key), Is.True);
            Assert.That(result.Any(a => a.Id == _attributeMap[Constants.ZephyrStatusAttribute].Id && (string)a.Value! == "Approved"), Is.True);
            Assert.That(result.Any(a => a.Id == _attributeMap["CustomField1"].Id && (string)a.Value! == "Value1"), Is.True);
            Assert.That(result.Any(a => a.Id == _attributeMap["CustomField2"].Id && (string)a.Value! == "Value2"), Is.True);
            Assert.That(result.Any(a => a.Id == _attributeMap["CheckboxAttribute"].Id && (string)a.Value! == "False"), Is.True);
        });

        _mockMappingConfigReader.Verify(m => m.InitOnce("mapping.json", ""), Times.Once);
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    public void CalculateAttributes_WithNullOrEmptyComponent_DoesNotAddComponentAttribute(string? component)
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(component: component);

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.That(result.Any(a => a.Id == _attributeMap[Constants.ComponentAttribute].Id), Is.False);
    }

    [Test]
    public void CalculateAttributes_WithoutComponent_DoesNotAddComponentAttribute()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(component: null);

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.That(result.Any(a => a.Id == _attributeMap[Constants.ComponentAttribute].Id), Is.False);
    }

    [Test]
    public void CalculateAttributes_WithNullCustomFields_HandlesGracefully()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase();
        zephyrTestCase.CustomFields = null;

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(a => a.Id == _attributeMap[Constants.IdZephyrAttribute].Id || a.Id == _attributeMap[Constants.ZephyrStatusAttribute].Id), Is.EqualTo(2));
        });

        _mockDetailedLogService.Verify(s => s.LogDebug(
            It.Is<string>(msg => msg.Contains("no custom fields")),
            It.IsAny<object[]>()), Times.Once);
    }

    [Test]
    public void CalculateAttributes_WithEmptyCustomFields_HandlesGracefully()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase();
        zephyrTestCase.CustomFields = new Dictionary<string, object>();

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockDetailedLogService.Verify(s => s.LogDebug(
            It.Is<string>(msg => msg.Contains("no custom fields")),
            It.IsAny<object[]>()), Times.Once);
    }

    [Test]
    public void CalculateAttributes_WithCustomFieldsNotInAttributeMap_SkipsThem()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(
            customFields: new Dictionary<string, object>
            {
                { "UnknownField", "Value" }
            });

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
            Assert.That(result.Any(a => (string)a.Value! == "Value"), Is.False);
        _mockDetailedLogService.Verify(s => s.LogDebug(
            It.Is<string>(msg => msg.Contains("cannot be obtained from the attribute map")),
            It.IsAny<object[]>()), Times.Once);
    }

    [Test]
    public void CalculateAttributes_WithMultipleOptionsAttribute_ConvertsCorrectly()
    {
        // Arrange
        var multipleOptionsAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = "MultiSelectField",
            Type = AttributeType.MultipleOptions,
            IsRequired = false,
            IsActive = true,
            Options = new List<string> { "Option1", "Option2", "Option3" }
        };
        _attributeMap["MultiSelectField"] = multipleOptionsAttribute;

        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(
            customFields: new Dictionary<string, object>
            {
                { "MultiSelectField", "Option1, Option2" }
            });

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        var multiSelectAttribute = result.FirstOrDefault(a => a.Id == multipleOptionsAttribute.Id);
        Assert.Multiple(() =>
        {
            Assert.That(multiSelectAttribute, Is.Not.Null);
            Assert.That(multiSelectAttribute!.Value, Is.InstanceOf<List<string>>());

            var values = (List<string>)multiSelectAttribute.Value!;
            Assert.That(values, Contains.Item("Option1"));
            Assert.That(values, Contains.Item("Option2"));
            Assert.That(values, Does.Not.Contain("Option3"));
        });

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting multiple value")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    [TestCase("Option1", "Option1, Option2")]
    [TestCase("Option2", "Option1, Option2")]
    [TestCase("Option1", "Option1")]
    public void CalculateAttributes_WithMultipleOptionsDifferentFormats_ConvertsCorrectly(string expectedOption, string attributeValue)
    {
        // Arrange
        var multipleOptionsAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = "MultiSelectField",
            Type = AttributeType.MultipleOptions,
            IsRequired = false,
            IsActive = true,
            Options = new List<string> { "Option1", "Option2" }
        };
        _attributeMap["MultiSelectField"] = multipleOptionsAttribute;

        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(
            customFields: new Dictionary<string, object>
            {
                { "MultiSelectField", attributeValue }
            });

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        var multiSelectAttribute = result.FirstOrDefault(a => a.Id == multipleOptionsAttribute.Id);
        Assert.That(multiSelectAttribute, Is.Not.Null);
        var values = (List<string>)multiSelectAttribute!.Value!;
        Assert.That(values, Contains.Item(expectedOption));
    }

    [Test]
    public void CalculateAttributes_WithRequiredAttributesNotUsed_SetsThemAsNotRequired()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase();
        zephyrTestCase.CustomFields = new Dictionary<string, object>();
        var requiredAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = "RequiredAttribute",
            Type = AttributeType.String,
            IsRequired = true,
            IsActive = true,
            Options = new List<string>()
        };
        _attributeMap["RequiredAttribute"] = requiredAttribute;
        _requiredAttributeNames = new List<string> { "RequiredAttribute" };

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.That(_attributeMap["RequiredAttribute"].IsRequired, Is.False);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Required attribute") && v.ToString()!.Contains("is not used")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void CalculateAttributes_WithUnusedCheckboxAttributes_AddsThemWithFalseValue()
    {
        // Arrange
        var checkboxAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = "UnusedCheckbox",
            Type = AttributeType.Checkbox,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };
        _attributeMap["UnusedCheckbox"] = checkboxAttribute;

        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase();
        zephyrTestCase.CustomFields = new Dictionary<string, object>();

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        var checkboxResult = result.FirstOrDefault(a => a.Id == checkboxAttribute.Id);
        Assert.Multiple(() =>
        {
            Assert.That(checkboxResult, Is.Not.Null);
            Assert.That(checkboxResult!.Value, Is.EqualTo("False"));
        });
    }

    [Test]
    public void CalculateAttributes_WithStatusMapping_RemapsStatus()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(status: "Approved");

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue("Approved", "Состояние"))
            .Returns("Ready");

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(zephyrTestCase.Status, Is.EqualTo("Ready"));
            Assert.That(result.Any(a => a.Id == _attributeMap[Constants.ZephyrStatusAttribute].Id && (string)a.Value == "Ready"), Is.True);
        });

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Map") && v.ToString()!.Contains("to")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void CalculateAttributes_WithMappingConfigReaderException_HandlesGracefully()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(status: "Approved");

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Mapping error"));

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.That(result, Is.Not.Null);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error while mapping attribute value")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void CalculateAttributes_WithNullCustomFieldValue_ConvertsToEmptyString()
    {
        // Arrange
        var customFieldAttribute = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = "CustomField",
            Type = AttributeType.String,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };
        _attributeMap["CustomField"] = customFieldAttribute;

        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(
            customFields: new Dictionary<string, object>
            {
                { "CustomField", null! }
            });

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        var result = _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        var customFieldResult = result.FirstOrDefault(a => a.Id == customFieldAttribute.Id);
        Assert.That(customFieldResult!.Value, Is.EqualTo(string.Empty));
    }

    [Test]
    public void CalculateAttributes_UpdatesRequiredAttributeNamesList()
    {
        // Arrange
        var zephyrTestCase = TestDataHelper.CreateZephyrTestCase(
            customFields: new Dictionary<string, object>
            {
                { "RequiredAttribute", "Value" }
            });

        _requiredAttributeNames = new List<string> { "RequiredAttribute" };

        _mockMappingConfigReader
            .Setup(m => m.InitOnce(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        _mockMappingConfigReader
            .Setup(m => m.GetMappingForValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => value);

        // Act
        _testCaseAttributesService.CalculateAttributes(
            zephyrTestCase,
            _attributeMap,
            _requiredAttributeNames);

        // Assert
        Assert.That(_requiredAttributeNames, Contains.Item("RequiredAttribute"));
        Assert.That(_attributeMap["RequiredAttribute"].IsRequired, Is.True);
    }

    #endregion
}
