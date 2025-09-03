using Microsoft.Extensions.Logging;
using Importer.Client;
using Importer.Services.Implementations;
using Importer.Models;
using Importer.Services;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ImporterTests;

public class TestCaseServiceTests
{
    private ILogger<TestCaseService> _logger = null!;
    private IClientAdapter _clientAdapter = null!;
    private IAdapterHelper _adapterHelper = null!;
    private IParserService _parserService = null!;
    private IParameterService _parameterService = null!;
    private IAttachmentService _attachmentService = null!;
    private IBaseWorkItemService _baseWorkItemService = null!;
    private TestCaseService _testCaseService = null!;

    private static readonly Guid ProjectId = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee7");
    private static readonly Guid SectionId = Guid.Parse("9f3c5ed5-d7d4-483f-b69f-e68c079cffe8");
    private static readonly Guid NewSectionId = Guid.Parse("7d1a4ec6-e8c5-494f-c70f-f79d168dffe9");

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<TestCaseService>>();
        _clientAdapter = Substitute.For<IClientAdapter>();
        _adapterHelper = Substitute.For<IAdapterHelper>();
        _parserService = Substitute.For<IParserService>();
        _parameterService = Substitute.For<IParameterService>();
        _attachmentService = Substitute.For<IAttachmentService>();
        _baseWorkItemService = Substitute.For<IBaseWorkItemService>();
        _testCaseService = new TestCaseService(_logger, _clientAdapter, _adapterHelper, _parserService,
            _parameterService, _baseWorkItemService, _attachmentService);
    }

    [Test]
    public async Task ImportTestCases_WhenNoTestCases_DoesNothing()
    {
        // Act
        await _testCaseService.ImportTestCases(
            ProjectId,
            Array.Empty<Guid>(),
            new Dictionary<Guid, Guid>(),
            new Dictionary<Guid, TmsAttribute>(),
            new Dictionary<Guid, Guid>()
        );

        // Assert
        Assert.Multiple(async () =>
        {
            await _parserService.DidNotReceive().GetTestCase(Arg.Any<Guid>());
            await _clientAdapter.DidNotReceive().ImportTestCase(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<TmsTestCase>());
        });
    }

    [Test]
    public async Task ImportTestCases_WhenSimpleTestCase_ImportsSuccessfully()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testCase = new TestCase
        {
            Id = testCaseId,
            Name = "Test Case",
            SectionId = SectionId,
            Steps = new List<Step> { new() { Action = "Test Action", Expected = "Test Expected" } },
            Iterations = new List<Iteration> { new() { Parameters = [] } }
        };

        var sectionsMap = new Dictionary<Guid, Guid> { { SectionId, NewSectionId } };

        _parserService.GetTestCase(testCaseId).Returns(testCase);
        _attachmentService.GetAttachments(testCaseId, Arg.Any<IEnumerable<string>>())
            .Returns(new Dictionary<string, Guid>());
        _parameterService.CreateParameters(Arg.Any<IEnumerable<Parameter>>())
            .Returns(new List<TmsParameter>());

        // Act
        await _testCaseService.ImportTestCases(
            ProjectId,
            new[] { testCaseId },
            sectionsMap,
            new Dictionary<Guid, TmsAttribute>(),
            new Dictionary<Guid, Guid>()
        );

        // Assert
        Assert.Multiple(async () =>
        {
            await _parserService.Received(1).GetTestCase(testCaseId);
            await _attachmentService.Received(1).GetAttachments(testCaseId, Arg.Any<IEnumerable<string>>());
            await _parameterService.Received(1).CreateParameters(Arg.Any<IEnumerable<Parameter>>());
            await _clientAdapter.Received(1).ImportTestCase(
                ProjectId,
                NewSectionId,
                Arg.Is<TmsTestCase>(tc =>
                    tc.Name == testCase.Name &&
                    tc.Steps.Count == 1 &&
                    tc.TmsIterations.Count == 1
                )
            );
        });
    }

    [Test]
    public async Task ImportTestCases_WhenTestCaseWithParameters_ImportsWithParametersReplaced()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var parameterKeyId = Guid.NewGuid();
        var testCase = new TestCase
        {
            Id = testCaseId,
            Name = "Test Case",
            SectionId = SectionId,
            Steps = new List<Step>
            {
                new()
                {
                    Action = "Action with <<<Parameter1>>>",
                    Expected = "Expected with <<<Parameter1>>>"
                }
            },
            Iterations = new List<Iteration>
            {
                new()
                {
                    Parameters =
                    [
                        new Parameter()
                        {
                            Name = "Parameter1",
                            Value = "Value1"
                        }
                    ]
                }
            }
        };

        var sectionsMap = new Dictionary<Guid, Guid> { { SectionId, NewSectionId } };
        var parameters = new List<TmsParameter>
        {
            new()
            {
                Name = "Parameter1",
                Value = "Value1",
                ParameterKeyId = parameterKeyId
            }
        };

        _parserService.GetTestCase(testCaseId).Returns(testCase);
        _attachmentService.GetAttachments(testCaseId, Arg.Any<IEnumerable<string>>())
            .Returns(new Dictionary<string, Guid>());
        _parameterService.CreateParameters(Arg.Any<IEnumerable<Parameter>>())
            .Returns(parameters);

        // Act
        await _testCaseService.ImportTestCases(
            ProjectId,
            new[] { testCaseId },
            sectionsMap,
            new Dictionary<Guid, TmsAttribute>(),
            new Dictionary<Guid, Guid>()
        );

        // Assert
        await _clientAdapter.Received(1).ImportTestCase(
            ProjectId,
            NewSectionId,
            Arg.Do<TmsTestCase>(tc =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(tc.Steps[0].Action, Does.Contain(parameterKeyId.ToString()));
                    Assert.That(tc.Steps[0].Expected, Does.Contain(parameterKeyId.ToString()));
                    Assert.That(tc.TmsIterations[0].Parameters, Does.Contain(parameterKeyId));
                });
            })
        );
    }

    [Test]
    public async Task ImportTestCases_WhenTestCaseWithSharedSteps_UpdatesSharedStepIds()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var sharedStepId = Guid.NewGuid();
        var newSharedStepId = Guid.NewGuid();

        var testCase = new TestCase
        {
            Id = testCaseId,
            Name = "Test Case",
            SectionId = SectionId,
            Steps = new List<Step>
            {
                new() { SharedStepId = sharedStepId }
            },
            Iterations = new List<Iteration> { new() { Parameters = [] } }
        };

        var sectionsMap = new Dictionary<Guid, Guid> { { SectionId, NewSectionId } };
        var sharedStepsMap = new Dictionary<Guid, Guid> { { sharedStepId, newSharedStepId } };

        _parserService.GetTestCase(testCaseId).Returns(testCase);
        _attachmentService.GetAttachments(testCaseId, Arg.Any<IEnumerable<string>>())
            .Returns(new Dictionary<string, Guid>());
        _parameterService.CreateParameters(Arg.Any<IEnumerable<Parameter>>())
            .Returns(new List<TmsParameter>());

        // Act
        await _testCaseService.ImportTestCases(
            ProjectId,
            new[] { testCaseId },
            sectionsMap,
            new Dictionary<Guid, TmsAttribute>(),
            sharedStepsMap
        );

        // Assert
        await _clientAdapter.Received(1).ImportTestCase(
            ProjectId,
            NewSectionId,
            Arg.Is<TmsTestCase>(tc =>
                tc.Steps[0].SharedStepId == newSharedStepId
            )
        );
    }

    [Test]
    public async Task ImportTestCases_WhenImportFails_LogsErrorAndContinues()
    {
        // Arrange
        var testCaseId = Guid.NewGuid();
        var testCase = new TestCase
        {
            Id = testCaseId,
            Name = "Test Case",
            SectionId = SectionId,
            Steps = new List<Step>(),
            Iterations = new List<Iteration> { new() { Parameters = [] } }
        };

        var exception = new ArgumentNullException("source", "Value cannot be null.");
        _parserService.GetTestCase(testCaseId).Returns(testCase);
        _attachmentService.GetAttachments(testCaseId, Arg.Any<IEnumerable<string>>())
            .Returns(new Dictionary<string, Guid>());
        _parameterService.CreateParameters(Arg.Any<IEnumerable<Parameter>>())
            .Returns(new List<TmsParameter>());
        _clientAdapter.ImportTestCase(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<TmsTestCase>())
            .Throws(exception);

        // Act
        await _testCaseService.ImportTestCases(
            ProjectId,
            new[] { testCaseId },
            new Dictionary<Guid, Guid> { { SectionId, NewSectionId } },
            new Dictionary<Guid, TmsAttribute>(),
            new Dictionary<Guid, Guid>()
        );

        // Assert
        _logger.Received().Log(
            Arg.Is<LogLevel>(l => l == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(testCase.Name) && o.ToString()!.Contains(exception.Message)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>()
        );
    }
}
