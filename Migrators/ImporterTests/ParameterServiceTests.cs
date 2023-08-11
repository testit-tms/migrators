using Importer.Client;
using Importer.Models;
using Importer.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ImporterTests;

public class ParameterServiceTests
{
    private ILogger<ParameterService> _logger;
    private IClient _client;
    private Parameter[] _parameters;
    private List<TmsParameter> _tmsParameters;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ParameterService>>();
        _client = Substitute.For<IClient>();
        _parameters = new[]
        {
            new Parameter()
            {
                Name = "Parameter1",
                Value = "Value1"
            },
            new Parameter()
            {
                Name = "Parameter2",
                Value = "Value2"
            }
        };
        _tmsParameters = new List<TmsParameter>
        {
            new TmsParameter()
            {
                Id = Guid.NewGuid(),
                Name = "Parameter1",
                Value = "Value1",
                ParameterKeyId = Guid.NewGuid()
            },
            new TmsParameter()
            {
                Id = Guid.NewGuid(),
                Name = "Parameter2",
                Value = "Value2",
                ParameterKeyId = Guid.NewGuid()
            }
        };
    }

    [Test]
    public async Task CreateParameters_FailedGetParameter()
    {
        // Arrange
        _client.GetParameter(_parameters[0].Name).ThrowsAsync(new Exception("Failed to get parameter"));

        var parameterService = new ParameterService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async ()=> await parameterService.CreateParameters(_parameters));

        // Assert
        await _client.DidNotReceive().CreateParameter(Arg.Any<Parameter>());
    }

    [Test]
    public async Task CreateParameters_FailedCreateParameter()
    {
        // Arrange
        _client.GetParameter(_parameters[0].Name).Returns(new List<TmsParameter>());
        _client.CreateParameter(_parameters[0]).ThrowsAsync(new Exception("Failed to create parameter"));
        var parameterService = new ParameterService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async ()=> await parameterService.CreateParameters(new []{_parameters[0]}));
    }

    [Test]
    public async Task CreateParameters_CreateParameterSuccess()
    {
        // Arrange
        _client.GetParameter(_parameters[0].Name).Returns(new List<TmsParameter>());
        _client.GetParameter(_parameters[1].Name).Returns(new List<TmsParameter>
        {
            _tmsParameters[1]
        });
        _client.CreateParameter(_parameters[0]).Returns(_tmsParameters[0]);
        var parameterService = new ParameterService(_logger, _client);

        // Act
        var resp = await parameterService.CreateParameters(_parameters);

        // Assert
        Assert.That(resp, Is.EqualTo(_tmsParameters));
    }
}
