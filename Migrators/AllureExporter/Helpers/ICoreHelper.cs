using Models;

namespace AllureExporter.Helpers;

public interface ICoreHelper
{
    void ExcludeLongTags(TestCase testcase);

    void ExcludeLongTags(SharedStep sharedStep);

    void CutLongTags(TestCase testcase);

    void CutLongTags(SharedStep sharedStep);
}
