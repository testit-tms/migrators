using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TestCollabExporter.Client;
using TestCollabExporter.Models;
using TestCollabExporter.Services;

namespace TestCollabExporterTests;

public class SharedStepServiceTests
{
    private ILogger<SharedStepService> _logger;
    private IClient _client;

    private const int ProjectId = 1;
    private readonly Guid _sectionId = Guid.NewGuid();
    private readonly List<Guid> _attributes = new() { Guid.NewGuid() };

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SharedStepService>>();
        _client = Substitute.For<IClient>();
    }

    [Test]
    public async Task ConvertSharedSteps_FailedGetSharedSteps()
    {
        // Arrange
        _client.GetSharedSteps(ProjectId)
            .Throws(new Exception("Failed to get shared steps"));

        var sharedStepService = new SharedStepService(_logger, _client);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await sharedStepService.ConvertSharedSteps(ProjectId, _sectionId, _attributes));
    }

    [Test]
    public async Task ConvertSharedSteps_Success()
    {
        // Arrange
        var testCollabSharedSteps = new List<TestCollabSharedStep>
        {
            new()
            {
                Id = 1,
                Name = "Shared Step 1",
                Steps = new List<Steps>
                {
                    new()
                    {
                        Step = "Step 1",
                        ExpectedResult = "Expected Result 1"
                    },
                    new()
                    {
                        Step = "Step 2",
                        ExpectedResult = "Expected Result 2"
                    }
                }
            },
            new()
            {
                Id = 2,
                Name = "Shared Step 2",
                Steps = new List<Steps>
                {
                    new()
                    {
                        Step = "Step 1",
                        ExpectedResult = "Expected Result 1"
                    },
                    new()
                    {
                        Step = "Step 2",
                        ExpectedResult = "Expected Result 2"
                    }
                }
            }
        };

        _client.GetSharedSteps(ProjectId)
            .Returns(testCollabSharedSteps);

        var sharedStepService = new SharedStepService(_logger, _client);

        // Act
        var sharedStepData = await sharedStepService.ConvertSharedSteps(ProjectId, _sectionId, _attributes);

        // Assert
        Assert.That(sharedStepData.SharedSteps, Has.Count.EqualTo(2));
        Assert.That(sharedStepData.SharedStepsMap, Has.Count.EqualTo(2));
        Assert.That(sharedStepData.SharedSteps[0].Name, Is.EqualTo("Shared Step 1"));
        Assert.That(sharedStepData.SharedSteps[0].Steps, Has.Count.EqualTo(2));
        Assert.That(sharedStepData.SharedSteps[0].Steps[0].Action, Is.EqualTo("Step 1"));
        Assert.That(sharedStepData.SharedSteps[0].Steps[0].Expected, Is.EqualTo("Expected Result 1"));
        Assert.That(sharedStepData.SharedSteps[0].Steps[1].Action, Is.EqualTo("Step 2"));
        Assert.That(sharedStepData.SharedSteps[0].Steps[1].Expected, Is.EqualTo("Expected Result 2"));
        Assert.That(sharedStepData.SharedSteps[1].Name, Is.EqualTo("Shared Step 2"));
        Assert.That(sharedStepData.SharedSteps[1].Steps, Has.Count.EqualTo(2));
        Assert.That(sharedStepData.SharedSteps[1].Steps[0].Action, Is.EqualTo("Step 1"));
        Assert.That(sharedStepData.SharedSteps[1].Steps[0].Expected, Is.EqualTo("Expected Result 1"));
        Assert.That(sharedStepData.SharedSteps[1].Steps[1].Action, Is.EqualTo("Step 2"));
        Assert.That(sharedStepData.SharedSteps[1].Steps[1].Expected, Is.EqualTo("Expected Result 2"));
    }
}
