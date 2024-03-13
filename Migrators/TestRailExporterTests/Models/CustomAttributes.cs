using TestRailExporter.Models;

namespace TestRailExporterTests.Models;

public readonly record struct CustomAttributes(List<CustomAttributeModel> Attributes);
