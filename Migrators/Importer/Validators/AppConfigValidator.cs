using Importer.Models;
using Microsoft.Extensions.Options;

namespace Importer.Validators;

public class AppConfigValidator : IValidateOptions<AppConfig>
{
    private const int DefaultTimeoutInSec = 10 * 60;

    public ValidateOptionsResult Validate(string? name, AppConfig options)
    {
        if (string.IsNullOrEmpty(options.ResultPath))
            return ValidateOptionsResult.Fail("ResultPath cannot be empty.");

        if (string.IsNullOrEmpty(options.Tms.Url) || !Uri.IsWellFormedUriString(options.Tms.Url, UriKind.Absolute))
            return ValidateOptionsResult.Fail("Tms.Url must be a valid URL.");

        if (options.Tms.Timeout == 0) options.Tms.Timeout = DefaultTimeoutInSec;

        return ValidateOptionsResult.Success;
    }
}