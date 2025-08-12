using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Validators;


public class AppConfigValidator : IValidateOptions<AppConfig>
{
    
    public ValidateOptionsResult Validate(string? name, AppConfig options)
    {
        if (string.IsNullOrEmpty(options.ResultPath))
            throw new ArgumentException("Result path is not specified");
        
        if (string.IsNullOrEmpty(options.Zephyr.Url)) 
            throw new ArgumentException("Url is not specified");
        if (string.IsNullOrEmpty(options.Zephyr.Confluence)) 
            throw new ArgumentException("Confluence baseUrl is not specified, " +
                                        "setup \"confluence\" in \"zephyr\" section for proper Confluence API usage");
        if (string.IsNullOrEmpty(options.Zephyr.ProjectKey)) 
            throw new ArgumentException("Project key is not specified");
        
        if (options.Zephyr.Partial)
        {
            if (string.IsNullOrEmpty(options.Zephyr.PartialFolderName)) 
                throw new ArgumentException("partialFolderName is not specified");
            if (options.Zephyr.Count == 0) 
                throw new ArgumentException("count cannot be zero or null");
        }
        

        return ValidateOptionsResult.Success;
    }
}