using AllureExporter.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter.Helpers;

internal class CoreHelper(ILogger<CoreHelper> logger) : ICoreHelper
{

    public void ExcludeLongTags(TestCase testcase)
    {
        testcase.Tags = testcase.Tags.Where(x => {
            if (x.Length <= 30) return true;
            logger.LogWarning("Tag " + x + " in " + testcase.Name + " is longer than 30 symbols, skipping...");
            return false;
        }).ToList();
    }

    public void ExcludeLongTags(SharedStep sharedStep)
    {
        sharedStep.Tags = sharedStep.Tags.Where(x => {
            if (x.Length <= 30) return true;
            logger.LogWarning("Tag " + x + " in shared" + sharedStep.Name + " is longer than 30 symbols, skipping...");
            return false;
        }).ToList();
    }


}
