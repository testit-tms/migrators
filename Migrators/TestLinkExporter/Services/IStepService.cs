using Models;
using TestLinkExporter.Models.Step;

namespace TestLinkExporter.Services;

public interface IStepService
{
    List<Step> ConvertSteps(List<TestLinkStep> testLinkSteps);
}
