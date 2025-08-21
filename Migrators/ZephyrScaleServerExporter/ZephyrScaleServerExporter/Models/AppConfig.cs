using System.ComponentModel.DataAnnotations;

namespace ZephyrScaleServerExporter.Models;


public class ZephyrConfig
{
    [Required]
    public string Url { get; set; } = string.Empty;
    [Required]
    public string ProjectKey { get; set; } = string.Empty;

    public string Confluence { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfluenceLogin { get; set; } = string.Empty;
    public string ConfluencePassword { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
    public string ConfluenceToken { get; set; } = string.Empty;
    public bool Partial { get; set; } = false;

    // start merge processing instead of export
    public bool Merge { get; set; } = false;
    public string FilterName { get; set; } = string.Empty;

    public string FilterSection { get; set; } = string.Empty;
    public int Count { get; set; } = 0;
    public string PartialFolderName { get; set; } = string.Empty;

    // this is reference to this filter: ["Неактуальный", "Не актуальный"];
    public bool IgnoreFilter { get; set; } = false;

    public bool ExportArchived { get; set; } = false;
}

public class AppConfig
{
    [Required]
    public string ResultPath { get; set; } = string.Empty;
    [Required]
    public ZephyrConfig Zephyr { get; set; } = new();

    public bool DetailedLog { get; set; } = false;
}
