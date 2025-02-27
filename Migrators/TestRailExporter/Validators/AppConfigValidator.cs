using Microsoft.Extensions.Options;
using TestRailExporter.Models.Client;

namespace ZephyrScaleServerExporter.Validators;


public class AppConfigValidator : IValidateOptions<AppConfig>
{
    public ValidateOptionsResult Validate(string? name, AppConfig options)
    {
        if (string.IsNullOrEmpty(options.ResultPath))
            throw new ArgumentException("Result path is not specified");

        if (string.IsNullOrEmpty(options.TestRail.Url))
            throw new ArgumentException("Url is not specified");
        if (string.IsNullOrEmpty(options.TestRail.ProjectName))
            throw new ArgumentException("Project name is not specified");

        return ValidateOptionsResult.Success;
    }
}
