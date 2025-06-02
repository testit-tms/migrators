using TestLinkExporter.Models.Config;
using Microsoft.Extensions.Options;

namespace TestLinkExporter.Validators;

internal class AppConfigValidator : IValidateOptions<AppConfig>
{
    public ValidateOptionsResult Validate(string? name, AppConfig options)
    {
        if (string.IsNullOrEmpty(options.ResultPath))
            return ValidateOptionsResult.Fail("ResultPath cannot be empty.");

        if (string.IsNullOrEmpty(options.TestLink.Url))
            return ValidateOptionsResult.Fail("Url cannot be empty.");

        if (string.IsNullOrEmpty(options.TestLink.Url) ||
            !Uri.IsWellFormedUriString(options.TestLink.Url, UriKind.Absolute))
            return ValidateOptionsResult.Fail("testlink.url must be a valid URL.");

        if (string.IsNullOrEmpty(options.TestLink.ProjectName))
            return ValidateOptionsResult.Fail("testlink.projectName is not specified");

        if (string.IsNullOrEmpty(options.TestLink.Token))
            ValidateOptionsResult.Fail("testlink.token must be specified");

        return ValidateOptionsResult.Success;
    }
}
