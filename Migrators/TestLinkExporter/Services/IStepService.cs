using Models;
using TestLinkExporter.Models;

namespace TestLinkExporter.Services;

public interface IStepService
{
    List<Step> ConvertSteps(List<TestLinkStep> testLinkSteps);
}
