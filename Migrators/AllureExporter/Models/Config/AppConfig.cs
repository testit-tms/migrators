using System.ComponentModel.DataAnnotations;

namespace AllureExporter.Models.Config;

internal class AppConfig
{
    [Required] public string ResultPath { get; set; } = string.Empty;

    [Required] public AllureConfig Allure { get; set; } = new();
}

internal class AllureConfig
{
    [Required] public string Url { get; set; } = string.Empty;

    [Required] public string ProjectName { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;

    // optional:
    public bool MigrateAutotests { get; set; } = false;
}
