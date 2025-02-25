using TestRailExporter.Models;

namespace TestRailXmlExporterTests.Models;

public readonly record struct CustomAttributes(List<CustomAttributeModel> Attributes);
