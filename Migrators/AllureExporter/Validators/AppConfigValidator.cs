using AllureExporter.Models.Config;
using Microsoft.Extensions.Options;

namespace AllureExporter.Validators;

internal class AppConfigValidator : IValidateOptions<AppConfig>
{
    public ValidateOptionsResult Validate(string? name, AppConfig options)
    {
        if (string.IsNullOrEmpty(options.ResultPath))
            return ValidateOptionsResult.Fail("ResultPath cannot be empty.");

        if (string.IsNullOrEmpty(options.Allure.Url))
            return ValidateOptionsResult.Fail("Url cannot be empty.");

        if (string.IsNullOrEmpty(options.Allure.Url) ||
            !Uri.IsWellFormedUriString(options.Allure.Url, UriKind.Absolute))
            return ValidateOptionsResult.Fail("allure.url must be a valid URL.");

        if (string.IsNullOrEmpty(options.Allure.ProjectName))
            return ValidateOptionsResult.Fail("allure.projectName is not specified");

        if (string.IsNullOrEmpty(options.Allure.ApiToken) && string.IsNullOrEmpty(options.Allure.BearerToken))
            ValidateOptionsResult.Fail("allure.apiToken or allure.bearerToken must be specified");

        return ValidateOptionsResult.Success;
    }
}
