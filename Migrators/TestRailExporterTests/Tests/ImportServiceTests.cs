using FluentAssertions;
using FluentAssertions.Execution;
using Models;
using NUnit.Framework;
using System.Xml.Serialization;
using TestRailExporter.Models;
using TestRailExporter.Services;
using TestRailExporterTests.Models;
using TestRailExporterTests.Tests.Base;

namespace TestRailExporterTests.Tests;

public class ImportServiceTests : BaseTest
{
    private ImportService _importService;

    [SetUp]
    public void Setup()
    {
        _importService = new ImportService(new XmlSerializer(typeof(TestRailsXmlSuite)));
    }

    [Test]
    [TestCase("example.xml")]
    [TestCase("Тестовый набор для BVT-тестирования10.0.0.xml")]
    [Parallelizable(ParallelScope.All)]
    public async Task ImportXml_Positive(string inputXmlName)
    {
        // Arrange
        var inputXml = Path.Combine(inputDirectory, inputXmlName);

        // Act
        (var actualSuite, var actualAttributes) = await _importService.ImportXmlAsync(inputXml)
            .ConfigureAwait(false);

        // Assert
        using (new AssertionScope())
        {
            await AssertOrUpdateExpectedJsonAsync(actualSuite).ConfigureAwait(false);
            await AssertOrUpdateExpectedJsonAsync(new CustomAttributes(actualAttributes)).ConfigureAwait(false);
        }
    }

    [Test]
    [TestCase("C:/Users/petr.komissarov/Downloads/Тестовый набор.xml", typeof(FileNotFoundException))]
    [TestCase("C:/", typeof(UnauthorizedAccessException))]
    [TestCase("C:/example.json", typeof(FileNotFoundException))]
    [TestCase("C:/example", typeof(FileNotFoundException))]
    [TestCase("", typeof(ArgumentException))]
    [TestCase(null, typeof(ArgumentNullException))]
    [Parallelizable(ParallelScope.All)]
    public async Task ImportXml_Negative(string? inputXml, Type expectedException)
    {
        // Arrange
        var actualException = default(Exception?);

        // Act
        try
        {
            await _importService.ImportXmlAsync(inputXml).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            actualException = exception;
        }

        // Assert
        actualException.Should().BeOfType(expectedException);
    }
}
