using Importer.Client;
using Importer.Models;
using Importer.Services.Implementations;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ImporterTests;

public class ParameterServiceTests
{
    private ILogger<ParameterService> _logger = null!;
    private IClientAdapter _clientAdapter = null!;
    private ParameterService _parameterService = null!;

    private static readonly Guid ParameterId1 = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private static readonly Guid ParameterId2 = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad0d");

    private List<Parameter> _parameters = null!;
    private List<TmsParameter> _existingParameters = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ParameterService>>();
        _clientAdapter = Substitute.For<IClientAdapter>();

        InitializeTestData();

        _parameterService = new ParameterService(_logger, _clientAdapter);
    }

    private void InitializeTestData()
    {
        _parameters = new List<Parameter>
        {
            new()
            {
                Name = "TestParam1",
                Value = "Value1"
            },
            new()
            {
                Name = "TestParam2",
                Value = "Value2"
            }
        };

        _existingParameters = new List<TmsParameter>
        {
            new()
            {
                Id = ParameterId1,
                Name = "TestParam1",
                Value = "Value1"
            }
        };
    }

    [Test]
    public async Task CreateParameters_WhenGetParameterFails_ThrowsException()
    {
        // Arrange
        _clientAdapter.GetParameter(_parameters[0].Name)
            .ThrowsAsync(new Exception("Failed to get parameter"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _parameterService.CreateParameters(_parameters));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to get parameter"));
            _clientAdapter.DidNotReceive().CreateParameter(Arg.Any<Parameter>());
        });
    }

    [Test]
    public async Task CreateParameters_WhenCreateParameterFails_ThrowsException()
    {
        // Arrange
        _clientAdapter.GetParameter(_parameters[0].Name).Returns(new List<TmsParameter>());
        _clientAdapter.CreateParameter(_parameters[0])
            .ThrowsAsync(new Exception("Failed to create parameter"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _parameterService.CreateParameters(_parameters));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Failed to create parameter"));
            _clientAdapter.Received(1).GetParameter(_parameters[0].Name);
        });
    }

    [Test]
    public async Task CreateParameters_WhenParameterExists_ReturnsExistingParameter()
    {
        // Arrange
        _clientAdapter.GetParameter(_parameters[0].Name).Returns(_existingParameters);

        // Act
        var result = await _parameterService.CreateParameters(new[] { _parameters[0] });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(_existingParameters[0].Id));
            Assert.That(result[0].Name, Is.EqualTo(_existingParameters[0].Name));
            Assert.That(result[0].Value, Is.EqualTo(_existingParameters[0].Value));

            _clientAdapter.Received(1).GetParameter(_parameters[0].Name);
            _clientAdapter.DidNotReceive().CreateParameter(Arg.Any<Parameter>());
        });
    }

    [Test]
    public async Task CreateParameters_WhenParameterDoesNotExist_CreatesNewParameter()
    {
        // Arrange
        var newParameter = new TmsParameter
        {
            Id = ParameterId2,
            Name = _parameters[1].Name,
            Value = _parameters[1].Value
        };

        _clientAdapter.GetParameter(_parameters[1].Name).Returns(new List<TmsParameter>());
        _clientAdapter.CreateParameter(_parameters[1]).Returns(newParameter);

        // Act
        var result = await _parameterService.CreateParameters(new[] { _parameters[1] });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(newParameter.Id));
            Assert.That(result[0].Name, Is.EqualTo(newParameter.Name));
            Assert.That(result[0].Value, Is.EqualTo(newParameter.Value));

            _clientAdapter.Received(1).GetParameter(_parameters[1].Name);
            _clientAdapter.Received(1).CreateParameter(_parameters[1]);
        });
    }

    [Test]
    public async Task CreateParameters_WithMultipleParameters_HandlesAllCorrectly()
    {
        // Arrange
        var newParameter = new TmsParameter
        {
            Id = ParameterId2,
            Name = _parameters[1].Name,
            Value = _parameters[1].Value
        };

        _clientAdapter.GetParameter(_parameters[0].Name).Returns(_existingParameters);
        _clientAdapter.GetParameter(_parameters[1].Name).Returns(new List<TmsParameter>());
        _clientAdapter.CreateParameter(_parameters[1]).Returns(newParameter);

        // Act
        var result = await _parameterService.CreateParameters(_parameters);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));

            // Check first parameter
            Assert.That(result[0].Id, Is.EqualTo(_existingParameters[0].Id));
            Assert.That(result[0].Name, Is.EqualTo(_existingParameters[0].Name));
            Assert.That(result[0].Value, Is.EqualTo(_existingParameters[0].Value));

            // Check second
            Assert.That(result[1].Id, Is.EqualTo(newParameter.Id));
            Assert.That(result[1].Name, Is.EqualTo(newParameter.Name));
            Assert.That(result[1].Value, Is.EqualTo(newParameter.Value));

            _clientAdapter.Received(1).GetParameter(_parameters[0].Name);
            _clientAdapter.Received(1).GetParameter(_parameters[1].Name);
            _clientAdapter.DidNotReceive().CreateParameter(_parameters[0]);
            _clientAdapter.Received(1).CreateParameter(_parameters[1]);
        });
    }
}
