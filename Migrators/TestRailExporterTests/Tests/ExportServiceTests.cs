using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NUnit.Framework;
using TestRailExporter.Models;
using TestRailExporter.Services;
using TestRailExporterTests.Models;
using TestRailExporterTests.Tests.Base;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace TestRailExporterTests.Tests;

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
    public void Setup() => _exportService = new ExportService(
        _factory.CreateLogger<ExportService>(),
        new WriteService(_factory.CreateLogger<WriteService>(), _configuration)
    );

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_resultPath))
            Directory.Delete(_resultPath, true);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory?.Dispose();

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
        var customAttributesModel = await DeserializeFileAsync<CustomAttributes>(customAttributesJson).ConfigureAwait(false);

        var testRailsXmlSuiteJson = Path.Combine(testDirectory, $"{nameof(TestRailsXmlSuite)}.json");
        var testRailsXmlSuiteModel = await DeserializeFileAsync<TestRailsXmlSuite>(testRailsXmlSuiteJson).ConfigureAwait(false);
           
        // Act
        await _exportService.ExportProjectAsync(testRailsXmlSuiteModel, customAttributesModel.Attributes).ConfigureAwait(false);
        var actualRoot = await DeserializeFileAsync<Root>(mainJson).ConfigureAwait(false);

        foreach (var testCaseDirectory in Directory.GetDirectories(_resultPath))
        {
            foreach (var testCaseJson in Directory.GetFiles(testCaseDirectory, "*.json", SearchOption.AllDirectories))
            {
                var testCase = await DeserializeFileAsync<TestCase>(testCaseJson).ConfigureAwait(false);
                actualTestCases.Cases.Add(testCase);
            }
        }

        // Assert
        await Assert.MultipleAsync(async () =>
        {
            await AssertOrUpdateExpectedJsonAsync(actualRoot).ConfigureAwait(false);
            await AssertOrUpdateExpectedJsonAsync(actualTestCases).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [Test]
    [TestCaseSource(nameof(NegativeInputData))]
    public void ExportJson_Negative(CustomAttributes customAttributesModel, TestRailsXmlSuite testRailsXmlSuiteModel)
    {
        // Act
        var actualException = Assert.CatchAsync(async () => await _exportService
            .ExportProjectAsync(testRailsXmlSuiteModel, customAttributesModel.Attributes)
            .ConfigureAwait(false));

        // Assert
        actualException.Should().BeOfType<ArgumentNullException>();
    }
}
