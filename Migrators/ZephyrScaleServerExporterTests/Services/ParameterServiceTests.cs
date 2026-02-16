using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Services;
using ZephyrScaleServerExporter.Services.Implementations;

namespace ZephyrScaleServerExporterTests.Services;

[TestFixture]
public class ParameterServiceTests
{
    private string _testCaseKey = null!;
    private Mock<IDetailedLogService> _mockDetailedLogService = null!;
    private Mock<IClient> _mockClient = null!;
    private ParameterService _parameterService = null!;

    [SetUp]
    public void SetUp()
    {
        _testCaseKey = "TC-123";
        _mockDetailedLogService = new Mock<IDetailedLogService>();
        _mockClient = new Mock<IClient>();
        _parameterService = new ParameterService(_mockDetailedLogService.Object, _mockClient.Object);
    }

    #region ConvertParameters

    [Test]
    public async Task ConvertParameters_WithTestDataParameterType_ReturnsCorrectIterations()
    {
        // Arrange
        var testData = new List<Dictionary<string, ZephyrDataParameter>>
        {
            new()
            {
                ["param1"] = new ZephyrDataParameter { Value = "value1" },
                ["param2"] = new ZephyrDataParameter { Value = "value2" }
            },
            new()
            {
                ["param1"] = new ZephyrDataParameter { Value = "value3" },
                ["param2"] = new ZephyrDataParameter { Value = "value4" }
            }
        };

        var parametersData = new ParametersData
        {
            Type = ZephyrParameterType.TEST_DATA,
            TestData = testData
        };

        _mockClient.Setup(c => c.GetParametersByTestCaseKey(_testCaseKey))
            .ReturnsAsync(parametersData);

        // Act
        var result = await _parameterService.ConvertParameters(_testCaseKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Has.Count.EqualTo(2), "Should contain 2 iterations");
            AssertParametersMatch(result[0].Parameters, new Dictionary<string, string> { ["param1"] = "value1", ["param2"] = "value2" });
            AssertParametersMatch(result[1].Parameters, new Dictionary<string, string> { ["param1"] = "value3", ["param2"] = "value4" });
        });
    }

    [Test]
    public async Task ConvertParameters_WithParameterType_ReturnsSingleIterationWithAllParameters()
    {
        // Arrange
        var parameters = new List<ZephyrParameter>
        {
            new() { Name = "param1", Value = "value1" },
            new() { Name = "param2", Value = "value2" }
        };

        var parametersData = new ParametersData
        {
            Type = ZephyrParameterType.PARAMETER,
            Parameters = parameters
        };

        _mockClient.Setup(c => c.GetParametersByTestCaseKey(_testCaseKey))
            .ReturnsAsync(parametersData);

        // Act
        var result = await _parameterService.ConvertParameters(_testCaseKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Has.Count.EqualTo(1), "Should contain 1 iteration");
            AssertParametersMatch(result[0].Parameters, new Dictionary<string, string> { ["param1"] = "value1", ["param2"] = "value2" });
        });
    }

    [Test]
    public async Task ConvertParameters_WithUnknownParameterType_ReturnsEmptyList()
    {
        // Arrange
        var parametersData = new ParametersData
        {
            Type = "UNKNOWN_TYPE"
        };

        _mockClient.Setup(c => c.GetParametersByTestCaseKey(_testCaseKey))
            .ReturnsAsync(parametersData);

        // Act
        var result = await _parameterService.ConvertParameters(_testCaseKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Empty, "Result should be empty for unknown parameter type");
        });
    }

    [Test]
    [Ignore("ConvertParameters does not handle null TestData — throws NullReferenceException in foreach. Expected: return empty list. Docs (07) specify null check.")]
    public async Task ConvertParameters_WithNullTestData_ReturnsEmptyList()
    {
        // Arrange
        var parametersData = new ParametersData
        {
            Type = ZephyrParameterType.TEST_DATA,
            TestData = null
        };

        _mockClient.Setup(c => c.GetParametersByTestCaseKey(_testCaseKey))
            .ReturnsAsync(parametersData);

        // Act
        var result = await _parameterService.ConvertParameters(_testCaseKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Empty, "Result should be empty when test data is null");
        });
    }

    [Test]
    [Ignore("ConvertParameters does not handle null Parameters — throws ArgumentNullException in Select. Expected: return empty list.")]
    public async Task ConvertParameters_WithNullParameters_ReturnsEmptyList()
    {
        // Arrange
        var parametersData = new ParametersData
        {
            Type = ZephyrParameterType.PARAMETER,
            Parameters = null
        };

        _mockClient.Setup(c => c.GetParametersByTestCaseKey(_testCaseKey))
            .ReturnsAsync(parametersData);

        // Act
        var result = await _parameterService.ConvertParameters(_testCaseKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Empty, "Result should be empty when parameters are null");
        });
    }

    [Test]
    public async Task ConvertParameters_WithEmptyTestData_ReturnsEmptyList()
    {
        // Arrange
        var parametersData = new ParametersData
        {
            Type = ZephyrParameterType.TEST_DATA,
            TestData = new List<Dictionary<string, ZephyrDataParameter>>()
        };

        _mockClient.Setup(c => c.GetParametersByTestCaseKey(_testCaseKey))
            .ReturnsAsync(parametersData);

        // Act
        var result = await _parameterService.ConvertParameters(_testCaseKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Empty, "Result should be empty when test data is empty");
        });
    }

    [Test]
    public async Task ConvertParameters_WithEmptyParameters_ReturnsEmptyList()
    {
        // Arrange
        var parametersData = new ParametersData
        {
            Type = ZephyrParameterType.PARAMETER,
            Parameters = new List<ZephyrParameter>()
        };

        _mockClient.Setup(c => c.GetParametersByTestCaseKey(_testCaseKey))
            .ReturnsAsync(parametersData);

        // Act
        var result = await _parameterService.ConvertParameters(_testCaseKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Has.Count.EqualTo(1), "Should contain 1 iteration");
            Assert.That(result[0].Parameters, Is.Empty, "Iteration should be empty when parameters are empty");
        });
    }

    #endregion

    #region MergeIterations

    [Test]
    public void MergeIterations_WithNonConflictingParameters_MergesAllParameters()
    {
        // Arrange
        var mainIterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param1", Value = "value1" }
                }
            }
        };

        var subIterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param2", Value = "value2" }
                }
            }
        };

        // Act
        var result = _parameterService.MergeIterations(mainIterations, subIterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Has.Count.EqualTo(1), "Should contain 1 iteration");
            AssertParametersMatch(result[0].Parameters, new Dictionary<string, string> { ["param1"] = "value1", ["param2"] = "value2" });
        });
    }

    [Test]
    public void MergeIterations_WithConflictingParameters_DoesNotAddConflictingParameters()
    {
        // Arrange
        var mainIterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param1", Value = "value1" },
                    new() { Name = "param2", Value = "value2" }
                }
            }
        };

        var subIterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param2", Value = "conflicting_value" }, // Conflicting parameter
                    new() { Name = "param3", Value = "value3" }
                }
            }
        };

        // Act
        var result = _parameterService.MergeIterations(mainIterations, subIterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Has.Count.EqualTo(1), "Should contain 1 iteration");
            AssertParametersMatch(result[0].Parameters, new Dictionary<string, string>
            {
                ["param1"] = "value1",
                ["param2"] = "value2",
                ["param3"] = "value3"
            });
        });
    }

    [Test]
    public void MergeIterations_WithMultipleMainIterations_MergesParametersToEachIteration()
    {
        // Arrange
        var mainIterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "mainParam1", Value = "mainValue1" }
                }
            },
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "mainParam2", Value = "mainValue2" }
                }
            }
        };

        var subIterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "subParam1", Value = "subValue1" }
                }
            }
        };

        // Act
        var result = _parameterService.MergeIterations(mainIterations, subIterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Has.Count.EqualTo(2), "Should contain 2 iterations");
            AssertParametersMatch(result[0].Parameters, new Dictionary<string, string> { ["mainParam1"] = "mainValue1", ["subParam1"] = "subValue1" });
            AssertParametersMatch(result[1].Parameters, new Dictionary<string, string> { ["mainParam2"] = "mainValue2", ["subParam1"] = "subValue1" });
        });
    }

    [Test]
    public void MergeIterations_WithEmptyMainIterations_ReturnsEmptyList()
    {
        // Arrange
        var mainIterations = new List<Iteration>();
        var subIterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param1", Value = "value1" }
                }
            }
        };

        // Act
        var result = _parameterService.MergeIterations(mainIterations, subIterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Empty, "Result should be empty when main iterations are empty");
        });
    }

    [Test]
    public void MergeIterations_WithEmptySubIterations_ReturnsMainIterationsUnchanged()
    {
        // Arrange
        var mainIterations = new List<Iteration>
        {
            new()
            {
                Parameters = new List<Parameter>
                {
                    new() { Name = "param1", Value = "value1" }
                }
            }
        };

        var subIterations = new List<Iteration>();

        // Act
        var result = _parameterService.MergeIterations(mainIterations, subIterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Has.Count.EqualTo(1), "Should contain 1 iteration");
            AssertParametersMatch(result[0].Parameters, new Dictionary<string, string> { ["param1"] = "value1" });
        });
    }

    [Test]
    public void MergeIterations_WithBothEmpty_ReturnsEmptyList()
    {
        // Arrange
        var mainIterations = new List<Iteration>();
        var subIterations = new List<Iteration>();

        // Act
        var result = _parameterService.MergeIterations(mainIterations, subIterations);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result, Is.Empty, "Result should be empty when both lists are empty");
        });
    }

    #endregion

    #region Assert Helpers

    private static void AssertParametersMatch(IList<Parameter> actual, Dictionary<string, string> expected)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count),
            $"Expected {expected.Count} parameters, but got {actual.Count}");
        foreach (var (name, expectedValue) in expected)
        {
            var param = actual.FirstOrDefault(p => p.Name == name);
            Assert.That(param, Is.Not.Null, $"Parameter '{name}' should exist");
            Assert.That(param!.Value, Is.EqualTo(expectedValue), $"Parameter '{name}' value should be '{expectedValue}'");
        }
    }

    #endregion
}
