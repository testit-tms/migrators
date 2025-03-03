using Models;

namespace Importer.Models;

public class TmsTestCase : TestCase
{
    public List<TmsIterations> TmsIterations { get; set; } = new();

    public static TmsTestCase Convert(TestCase testCase)
    {
        return new TmsTestCase
        {
            Id = testCase.Id,
            Description = testCase.Description,
            State = testCase.State,
            Priority = testCase.Priority,
            Steps = testCase.Steps,
            PreconditionSteps = testCase.PreconditionSteps,
            PostconditionSteps = testCase.PostconditionSteps,
            Duration = testCase.Duration,
            Attributes = testCase.Attributes,
            Tags = testCase.Tags,
            Attachments = testCase.Attachments,
            Iterations = testCase.Iterations,
            Links = testCase.Links,
            Name = testCase.Name,
            SectionId = testCase.SectionId
        };
    }
}