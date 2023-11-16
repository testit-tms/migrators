using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using PractiTestExporter.Client;
using PractiTestExporter.Models;
using PractiTestExporter.Services;
using Attribute = Models.Attribute;
using Step = Models.Step;
using TestCaseData = PractiTestExporter.Models.TestCaseData;

namespace PractiTestExporterTests;

public class ExportServiceTests
{
    private ILogger<ExportService> _logger;
    private IClient _client;
    private ITestCaseService _testCaseService;
    private IWriteService _writeService;
    private IAttributeService _attributeService;
    private PractiTestProject _project;
    private AttributeData _attributeData;
    private TestCaseData _testCaseData;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExportService>>();
        _client = Substitute.For<IClient>();
        _testCaseService = Substitute.For<ITestCaseService>();
        _writeService = Substitute.For<IWriteService>();
        _attributeService = Substitute.For<IAttributeService>();

        _project = new PractiTestProject
        {
            Data = new ProjectData
            {
                Id = "123",
                Attributes = new ProjectAttributes
                {
                    Name = "My project"
                }
            },
        };

        _attributeData = new AttributeData
        {
            Attributes = new List<Attribute>(),
            AttributeMap = new Dictionary<string, Guid>()
        };

        _testCaseData = new TestCaseData
        {
            SharedSteps = new List<SharedStep>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Shared step 1"
                }
            },
            TestCases = new List<TestCase>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Test case 1"
                }
            }
        };
    }

    [Test]
    public async Task ExportProject_FailedGetProject()
    {
        // Arrange
        _client.GetProject()
            .Throws(new Exception("Failed to get project"));

        var exportService = new ExportService(_logger, _client, _writeService, _testCaseService, _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Guid>(), Arg.Any<Dictionary<string, Guid>>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertCustomAttributes()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _attributeService.ConvertCustomAttributes()
            .Throws(new Exception("Failed to convert custom attributes"));

        var exportService = new ExportService(_logger, _client, _writeService, _testCaseService, _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _testCaseService.DidNotReceive()
            .ConvertTestCases(Arg.Any<Guid>(), Arg.Any<Dictionary<string, Guid>>());

        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedConvertTestCases()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _attributeService.ConvertCustomAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(Arg.Any<Guid>(), _attributeData.AttributeMap)
            .Throws(new Exception("Failed to convert test cases"));

        var exportService = new ExportService(_logger, _client, _writeService, _testCaseService, _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.DidNotReceive()
            .WriteSharedStep(Arg.Any<SharedStep>());

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteSharedStep()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _attributeService.ConvertCustomAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(Arg.Any<Guid>(), _attributeData.AttributeMap)
            .Returns(_testCaseData);

        _writeService.WriteSharedStep(_testCaseData.SharedSteps[0])
            .Throws(new Exception("Failed to write shared step"));

        var exportService = new ExportService(_logger, _client, _writeService, _testCaseService, _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert

        await _writeService.DidNotReceive()
            .WriteTestCase(Arg.Any<TestCase>());

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteTestCase()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _attributeService.ConvertCustomAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(Arg.Any<Guid>(), _attributeData.AttributeMap)
            .Returns(_testCaseData);

        _writeService.WriteTestCase(_testCaseData.TestCases[0])
            .Throws(new Exception("Failed to write test case"));

        var exportService = new ExportService(_logger, _client, _writeService, _testCaseService, _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.Received()
            .WriteSharedStep(_testCaseData.SharedSteps[0]);

        await _writeService.DidNotReceive()
            .WriteMainJson(Arg.Any<Root>());
    }

    [Test]
    public async Task ExportProject_FailedWriteMainJson()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _attributeService.ConvertCustomAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(Arg.Any<Guid>(), _attributeData.AttributeMap)
            .Returns(_testCaseData);

        _writeService.WriteMainJson(Arg.Any<Root>())
            .Throws(new Exception("Failed to write test case"));

        var exportService = new ExportService(_logger, _client, _writeService, _testCaseService, _attributeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () => await exportService.ExportProject());

        // Assert
        await _writeService.Received()
            .WriteSharedStep(_testCaseData.SharedSteps[0]);

        await _writeService.Received()
            .WriteTestCase(_testCaseData.TestCases[0]);
    }

    [Test]
    public async Task ExportProject_Success()
    {
        // Arrange
        _client.GetProject()
            .Returns(_project);

        _attributeService.ConvertCustomAttributes()
            .Returns(_attributeData);

        _testCaseService.ConvertTestCases(Arg.Any<Guid>(), _attributeData.AttributeMap)
            .Returns(_testCaseData);

        var exportService = new ExportService(_logger, _client, _writeService, _testCaseService, _attributeService);

        // Act
        await exportService.ExportProject();

        // Assert
        await _writeService.Received()
            .WriteSharedStep(_testCaseData.SharedSteps[0]);

        await _writeService.Received()
            .WriteTestCase(_testCaseData.TestCases[0]);

        await _writeService.Received()
            .WriteMainJson(Arg.Any<Root>());
    }
}
