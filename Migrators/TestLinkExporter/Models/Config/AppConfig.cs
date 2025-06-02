using System.ComponentModel.DataAnnotations;

namespace TestLinkExporter.Models.Config;

internal class AppConfig
{
    [Required] public string ResultPath { get; set; } = string.Empty;

    [Required] public TestLinkConfig TestLink { get; set; } = new();
}

internal class TestLinkConfig
{
    [Required] public string Url { get; set; } = string.Empty;

    [Required] public string ProjectName { get; set; } = string.Empty;

    [Required] public string Token { get; set; } = string.Empty;
}
