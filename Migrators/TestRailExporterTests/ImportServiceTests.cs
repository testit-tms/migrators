using FluentAssertions;
using Models;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Text;
using System.Xml.Serialization;
using TestRailExporter.Models;
using TestRailExporter.Services;
using TestRailExporterTests.Models;

namespace TestLinkExporterTests;

public class ImportServiceTests
{
    private static string _inputFolder;
    private ImportService _importService;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _inputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Input");

    [SetUp]
    public void Setup() => _importService = new ImportService(new XmlSerializer(typeof(TestRailsXmlSuite)));

    [Test]
    [TestCase("example.xml")]
    [TestCase("Тестовый набор для BVT-тестирования10.0.0.xml")]
    public async Task ImportXml_Positive(string inputXmlName)
    {
        // Arrange
        var inputXml = Path.Combine(_inputFolder, inputXmlName);

        // Act
        (var actualSuite, var actualAttributes) = await _importService.ImportXmlAsync(inputXml).ConfigureAwait(false);

        // Assert
        await Assert.MultipleAsync(async () =>
        {
            await AssertOrUpdateExpectedJson(actualSuite).ConfigureAwait(false);
            await AssertOrUpdateExpectedJson(new CustomAttributes(actualAttributes)).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [Test]
    [TestCase("C:/Users/petr.komissarov/Downloads/Тестовый набор для BVT-тестирования12.0.0.xml", typeof(FileNotFoundException))]
    [TestCase("C:/", typeof(UnauthorizedAccessException))]
    [TestCase("C:/example.json", typeof(FileNotFoundException))]
    [TestCase("C:/example", typeof(FileNotFoundException))]
    [TestCase("", typeof(ArgumentException))]
    [TestCase(null, typeof(ArgumentNullException))]
    public void ImportXml_Negative(string? inputXml, Type expectedException)
    {
        // Act & Assert
        var actualException = Assert.CatchAsync(async () => await _importService.ImportXmlAsync(inputXml)
            .ConfigureAwait(false));

        actualException.GetType().Should().Be(expectedException);
    }

    private static async Task AssertOrUpdateExpectedJson<T>(T actualModel) where T : notnull
    {
        var projectRootPath = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName!;

        var outputPath = Path.Combine(
            projectRootPath,
            "Output",
            new string(TestContext.CurrentContext.Test.Name.Where(
                c => !Path.GetInvalidPathChars().Contains(c) && !Path.GetInvalidFileNameChars().Contains(c)).ToArray())
        );

        Directory.CreateDirectory(outputPath);
        var outputJson = Path.Combine(outputPath, $"{typeof(T).Name}.json");

        if (File.Exists(outputJson))
        {
            var expectedText = await File.ReadAllTextAsync(outputJson, Encoding.UTF8).ConfigureAwait(false);
            var expectedModel = JsonConvert.DeserializeObject<T>(expectedText) ?? Activator.CreateInstance<T>();

            actualModel.Should().BeEquivalentTo(expectedModel);
        }
        else
            await File.WriteAllTextAsync(
                outputJson,
                JsonConvert.SerializeObject(actualModel),
                Encoding.UTF8).ConfigureAwait(false);
    }
}
