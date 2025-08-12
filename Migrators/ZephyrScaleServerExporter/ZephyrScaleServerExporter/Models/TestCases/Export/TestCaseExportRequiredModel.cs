using ZephyrScaleServerExporter.Models.Common;
using Attribute = Models.Attribute;

namespace ZephyrScaleServerExporter.Models.TestCases.Export;

public class TestCaseExportRequiredModel
{
    public Attribute OwnersAttribute { get; set; } = null!;
    public StatusData StatusData { get; set; } = null!;
    public List<string> RequiredAttributeNames { get; set; } = null!;
}

