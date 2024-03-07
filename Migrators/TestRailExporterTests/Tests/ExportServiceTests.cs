using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using NUnit.Framework;
using TestRailExporter.Models;
using TestRailExporter.Services;
using TestRailExporterTests.Models;
using TestRailExporterTests.Tests.Base;
using FluentAssertions;

namespace TestRailExporterTests.Tests;

public class ExportServiceTests : BaseTest
{
    private static readonly string _inputFolder = Path.Combine(projectRootPath, "Data", "Output");
    private static TestConfiguration _configuration;
    private static ILoggerFactory _factory;
    private ExportService _exportService;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _configuration = new TestConfiguration();
        _factory = LoggerFactory.Create(builder => builder.AddConsole());
    }

    [SetUp]
    public void Setup() => _exportService = new ExportService(_factory.CreateLogger<ExportService>(),
        new WriteService(_factory.CreateLogger<WriteService>(), _configuration));

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_configuration["resultPath"]!))
            Directory.Delete(_configuration["resultPath"]!, true);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory?.Dispose();

    private static IEnumerable<TestCaseData> PositiveInputData()
    {
        foreach (var directory in Directory.GetDirectories(_inputFolder, "ImportXml*"))
        {
            yield return new TestCaseData(Path.GetRelativePath(_inputFolder, directory));
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
        var mainJson = Path.Combine(_configuration["resultPath"]!, Constants.MainJson);

        var directory = Path.Combine(_inputFolder, directoryName);
        var customAttributesJson = Path.Combine(directory, $"{nameof(CustomAttributes)}.json");
        var customAttributesModel = DeserializeFile<CustomAttributes>(customAttributesJson);

        var testRailsXmlSuiteJson = Path.Combine(directory, $"{nameof(TestRailsXmlSuite)}.json");
        var testRailsXmlSuiteModel = DeserializeFile<TestRailsXmlSuite>(testRailsXmlSuiteJson);
           
        // Act
        await _exportService.ExportProjectAsync(testRailsXmlSuiteModel, customAttributesModel.Attributes).ConfigureAwait(false);
        var actualRoot = DeserializeFile<Root>(mainJson);
        actualTestCases.Cases.AddRange(from resultDir in Directory.GetDirectories(_configuration["resultPath"]!)
                                       from file in Directory.GetFiles(resultDir, "*.json", SearchOption.AllDirectories)
                                       let testCase = DeserializeFile<TestCase>(file)
                                       select testCase);

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
