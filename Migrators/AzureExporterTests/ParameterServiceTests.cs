using System.Xml;
using AzureExporter.Models;
using AzureExporter.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AzureExporterTests;

public class ParameterServiceTests
{
    private ILogger<ParameterService> _logger;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ParameterService>>();
    }

    [Test]
    public void ConvertParameters_KeysNotSpecified()
    {
        // Arrange
        var parameters = new AzureParameters
        {
            Keys = "",
            Values = ""
        };

        var parameterService = new ParameterService(_logger);

        // Act
        var result = parameterService.ConvertParameters(parameters);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ConvertParameters_KeysSpecified_ValuesNotSpecified()
    {
        // Arrange
        var parameters = new AzureParameters
        {
            Keys =
                "<parameters><param name=\"Login\" bind=\"default\" /><param name=\"Password\" bind=\"default\" /></parameters>",
            Values = ""
        };

        var parameterService = new ParameterService(_logger);

        // Act
        var result = parameterService.ConvertParameters(parameters);

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Has.Count.EqualTo(2));
        Assert.That(result[0]["Login"], Is.EqualTo("Empty"));
        Assert.That(result[0]["Password"], Is.EqualTo("Empty"));
    }

    [Test]
    public void ConvertParameters_KeysAndValuesSpecified()
    {
        // Arrange
        var parameters = new AzureParameters
        {
            Keys =
                "<parameters><param name=\"Login\" bind=\"default\" /><param name=\"Password\" bind=\"default\" /></parameters>",
            Values =
                "<NewDataSet><xs:schema id='NewDataSet' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'><xs:element name='NewDataSet' msdata:IsDataSet='true' msdata:Locale=''><xs:complexType> <xs:choice minOccurs='0' maxOccurs = 'unbounded'><xs:element name='Table1'><xs:complexType><xs:sequence><xs:element name='Login' type='xs:string' minOccurs='0' /><xs:element name='Password' type='xs:string' minOccurs='0' /></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><Table1><Login>gda</Login><Password>qwerty123</Password></Table1><Table1><Login>adq</Login><Password>rewq123</Password></Table1></NewDataSet>"
        };

        var parameterService = new ParameterService(_logger);

        // Act
        var result = parameterService.ConvertParameters(parameters);

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Has.Count.EqualTo(2));
        Assert.That(result[0]["Login"], Is.EqualTo("gda"));
        Assert.That(result[0]["Password"], Is.EqualTo("qwerty123"));
        Assert.That(result[1], Has.Count.EqualTo(2));
        Assert.That(result[1]["Login"], Is.EqualTo("adq"));
        Assert.That(result[1]["Password"], Is.EqualTo("rewq123"));
    }

    [Test]
    public void ConvertParameters_KeysAndValuesSpecifiedWithEmptyValues()
    {
        // Arrange
        var parameters = new AzureParameters
        {
            Keys =
                "<parameters><param name=\"Login\" bind=\"default\" /><param name=\"Password\" bind=\"default\" /></parameters>",
            Values =
                "<NewDataSet><xs:schema id='NewDataSet' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'><xs:element name='NewDataSet' msdata:IsDataSet='true' msdata:Locale=''><xs:complexType> <xs:choice minOccurs='0' maxOccurs = 'unbounded'><xs:element name='Table1'><xs:complexType><xs:sequence><xs:element name='Login' type='xs:string' minOccurs='0' /><xs:element name='Password' type='xs:string' minOccurs='0' /></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><Table1><Login></Login><Password>qwerty123</Password></Table1><Table1><Login>adq</Login><Password></Password></Table1></NewDataSet>"
        };

        var parameterService = new ParameterService(_logger);

        // Act
        var result = parameterService.ConvertParameters(parameters);

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Has.Count.EqualTo(2));
        Assert.That(result[0]["Login"], Is.EqualTo("Empty"));
        Assert.That(result[0]["Password"], Is.EqualTo("qwerty123"));
        Assert.That(result[1], Has.Count.EqualTo(2));
        Assert.That(result[1]["Login"], Is.EqualTo("adq"));
        Assert.That(result[1]["Password"], Is.EqualTo("Empty"));
    }

    [Test]
    public void ConvertParameters_KeysAndValuesSpecified_InvalidXml()
    {
        // Arrange
        var parameters = new AzureParameters
        {
            Keys =
                "<parameters><param name=\"Login\" bind=\"default\" /><param name=\"Password\" bind=\"default\" /></parameters>",
            Values =
                "<NewDataSet><xs:schema id='NewDataSet' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'><xs:element name='NewDataSet' msdata:IsDataSet='true' msdata:Locale=''><xs:complexType> <xs:choice minOccurs='0' maxOccurs = 'unbounded'><xs:element name='Table1'><xs:complexType><xs:sequence><xs:element name='Login' type='xs:string' minOccurs='0' /><xs:element name='Password' type='xs:string' minOccurs='0' /></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><Table1><Login>gda</Login><Password>qwerty123</Password></Table1><Table1><Login>adq</Login><Password>rewq123</Password></Table1>"
        };

        var parameterService = new ParameterService(_logger);

        // Act
        var result = parameterService.ConvertParameters(parameters);

        // Assert
        Assert.That(result, Is.Empty);
    }
}
