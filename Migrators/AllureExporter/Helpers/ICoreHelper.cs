using AllureExporter.Models;
using Models;

namespace AllureExporter.Helpers;

public interface ICoreHelper
{
    void ExcludeLongTags(TestCase testcase);

    void ExcludeLongTags(SharedStep sharedStep);
}
