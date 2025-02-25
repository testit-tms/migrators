using FluentAssertions;
using FluentAssertions.Execution;
using JsonWriter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using NUnit.Framework;
using TestRailXmlExporter.Models;
using TestRailXmlExporter.Services;
using TestRailXmlExporterTests.Models;
using TestRailXmlExporterTests.Tests.Base;

namespace TestRailXmlExporterTests.Tests;

public class ExportServiceTests : BaseTest
{
    private static IConfiguration _configuration;
    private static ILoggerFactory _factory;
    private static string _resultPath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _configuration = new TestConfiguration();
        _factory = LoggerFactory.Create(builder => builder.AddConsole());
        _resultPath = _configuration["resultPath"]!;
    }

    private ExportService _exportService;

    [SetUp]
    public void Setup()
    {
        var writeService = new WriteService(_factory.CreateLogger<WriteService>(), _configuration);
        _exportService = new ExportService(_factory.CreateLogger<ExportService>(), writeService);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_resultPath))
        {
            Directory.Delete(_resultPath, true);
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory?.Dispose();
    }

    private static IEnumerable<TestCaseData> PositiveInputData()
    {
        foreach (var testDirectory in Directory.GetDirectories(outputDirectory, "ImportXml*"))
        {
            yield return new TestCaseData(Path.GetRelativePath(outputDirectory, testDirectory));
        }
    }

    private static IEnumerable<TestCaseData> NegativeInputData()
    {
        yield return new TestCaseData(new CustomAttributes(), new TestRailsXmlSuite());
        yield return new TestCaseData(null, new TestRailsXmlSuite());
        yield return new TestCaseData(new CustomAttributes(), null);
        yield return new TestCaseData(null, null);
    }

    [Test]
    [TestCaseSource(nameof(PositiveInputData))]
    public async Task ExportJson_Positive(string directoryName)
    {
        // Arrange
        var actualTestCases = new TestCases(new List<TestCase>());
        var mainJson = Path.Combine(_resultPath, Constants.MainJson);

        var testDirectory = Path.Combine(outputDirectory, directoryName);
        var customAttributesJson = Path.Combine(testDirectory, $"{nameof(CustomAttributes)}.json");
        var customAttributesModel = await DeserializeFileAsync<CustomAttributes>(customAttributesJson)
            .ConfigureAwait(false);

        var testRailsXmlSuiteJson = Path.Combine(testDirectory, $"{nameof(TestRailsXmlSuite)}.json");
        var testRailsXmlSuiteModel = await DeserializeFileAsync<TestRailsXmlSuite>(testRailsXmlSuiteJson)
            .ConfigureAwait(false);

        // Act
        await _exportService.ExportProjectAsync(testRailsXmlSuiteModel, customAttributesModel.Attributes)
            .ConfigureAwait(false);
        var actualRoot = await DeserializeFileAsync<Root>(mainJson).ConfigureAwait(false);

        foreach (var testCaseDirectory in Directory.GetDirectories(_resultPath))
        {
            var testCasesJsons = Directory.GetFiles(testCaseDirectory, "*.json", SearchOption.AllDirectories);

            foreach (var testCaseJson in testCasesJsons)
            {
                var testCase = await DeserializeFileAsync<TestCase>(testCaseJson).ConfigureAwait(false);
                actualTestCases.Cases.Add(testCase);
            }
        }

        // Assert
        using (new AssertionScope())
        {
            await AssertOrUpdateExpectedJsonAsync(actualRoot).ConfigureAwait(false);
            await AssertOrUpdateExpectedJsonAsync(actualTestCases).ConfigureAwait(false);
        }
    }

    [Test]
    [TestCaseSource(nameof(NegativeInputData))]
    [Parallelizable(ParallelScope.All)]
    public async Task ExportJson_Negative(CustomAttributes customAttributes, TestRailsXmlSuite testRailsXmlSuite)
    {
        // Arrange
        var actualException = default(Exception?);

        // Act
        try
        {
            await _exportService.ExportProjectAsync(testRailsXmlSuite, customAttributes.Attributes)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            actualException = exception;
        }

        // Assert
        actualException.Should().BeOfType<ArgumentNullException>();
    }
}
