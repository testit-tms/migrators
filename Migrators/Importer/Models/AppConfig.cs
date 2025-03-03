using System.ComponentModel.DataAnnotations;

namespace Importer.Models;

public class AppConfig
{
    [Required] public string ResultPath { get; set; } = string.Empty;

    [Required] public TmsConfig Tms { get; set; } = new();
}

public class TmsConfig
{
    [Required] public string Url { get; set; } = string.Empty;

    [Required] public string PrivateToken { get; set; } = string.Empty;

    // optional:
    public bool CertValidation { get; set; } = true;
    public int Timeout { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool ImportToExistingProject { get; set; }
}