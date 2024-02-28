using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;
using TestRailImporter.Models;
using Attribute = Models.Attribute;
using Section = Models.Section;

namespace TestRailImporter.Services
{
    public class TestRailExportService
    {
        private static readonly List<Guid> _sharedStepsIds = new();
        private readonly ILogger<TestRailExportService> _logger;
        private readonly IWriteService _writeService;

        public TestRailExportService(ILogger<TestRailExportService> logger, IWriteService writeService)
        {
            _logger = logger;
            _writeService = writeService;
        }

        public async Task ExportProjectAsync(TestRailsXmlSuite testRailsXmlSuite, List<CustomAttributeModel> customAttributes)
        {
            _logger.LogInformation("Exporting project");

            var sectionData = ConvertSections(testRailsXmlSuite.Sections);
            var attributeData = ConvertAttributes(customAttributes);
            var testCaseData = ConvertTestCases(testRailsXmlSuite.Sections, sectionData);


            foreach (var sharedStepId in _sharedStepsIds)
            {
                var testCase = testCaseData.FirstOrDefault(testCase => _sharedStepsIds.Contains(testCase.Id));
                var sharedStep = ConvertTestCaseToSharedStep(testCase);

                if (sharedStep == null) continue;

                await _writeService.WriteSharedStep(sharedStep).ConfigureAwait(false);
            }

            foreach (var testCase in testCaseData)
            {
                await _writeService.WriteTestCase(testCase).ConfigureAwait(false);
            }

            var root = new Root
            {
                ProjectName = testRailsXmlSuite.Name ?? string.Empty,
                Attributes = attributeData,
                Sections = sectionData,
                SharedSteps = _sharedStepsIds,
                TestCases = testCaseData.Select(testCase => testCase.Id).ToList()
            };

            await _writeService.WriteMainJson(root).ConfigureAwait(false);

            _logger.LogInformation("Export completed");
        }

        private static List<Attribute> ConvertAttributes(IEnumerable<CustomAttributeModel> customAttributes)
        {
            var attributes = customAttributes.Select(customAttribute =>
            {
                var attribute = new Attribute
                {
                    Id = customAttribute.Id == Guid.Empty ? Guid.NewGuid() : customAttribute.Id,
                    Name = customAttribute.Name,
                    Type = (AttributeType)customAttribute.Type,
                    IsRequired = customAttribute.IsRequired,
                    IsActive = !customAttribute.IsDeleted,
                    Options = customAttribute.Options?.Select(option
                        => JsonConvert.SerializeObject(option)).ToList() ?? new List<string>()
                };

                return attribute;
            });


            return attributes.ToList();
        }

        private static List<Section> ConvertSections(IEnumerable<TestRailsXmlSection>? testRailSections)
        {
            var sections = testRailSections?.Select(testrailSection =>
            {
                var section = new Section
                {
                    Id = Guid.NewGuid(),
                    Name = testrailSection.Name ?? string.Empty,
                    Sections = ConvertSections(testrailSection.Sections),
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>()
                };

                return section;
            }).ToList();


            return sections ?? new List<Section>();
        }

        private static IEnumerable<TestCase> ConvertTestCases(TestRailsXmlSection[]? testRailSections, List<Section> sectionData)
        {
            
            var testCases = new List<TestCase>();

            if (testRailSections == null || testRailSections.Length == 0)
            {
                return testCases;
            }

            foreach (var section in testRailSections)
            {
                
                if (section.Cases == null || section.Cases.Length == 0)
                {
                    continue;
                }

                foreach (var testRailCase in section.Cases)
                {
                    var testCase = new TestCase
                    {
                        Id = Guid.TryParse(testRailCase.Id, out var guid) ? guid : Guid.NewGuid(),
                        State = Enum.TryParse(testRailCase.State, out StateType type) ? type : StateType.Ready,
                        Priority = Enum.TryParse(testRailCase.Priority, out PriorityType priority)
                            ? priority
                            : PriorityType.Medium,
                        Steps = new List<Step>(
                            testRailCase.Custom.GetValueOrDefault(new TestRailsXmlCaseData()).Steps.Select(ConvertStep)
                        ),
                        PreconditionSteps = new List<Step>()
                        {
                            ConvertStep(testRailCase.Custom.GetValueOrDefault(new TestRailsXmlCaseData()).Preconditions)
                        },
                        PostconditionSteps = new List<Step>(),
                        Duration = int.TryParse(testRailCase.Estimate, out var duration) ? duration : 0,
                        Attributes = new List<CaseAttribute>(),
                        Tags = new List<string>(),
                        Attachments = new List<string>(),
                        Iterations = new List<Iteration>(),
                        Links = new List<Link>(),
                        Name = testRailCase.Title ?? string.Empty,
                        SectionId = sectionData.FirstOrDefault(data => data.Name == section.Name)?.Id ?? Guid.NewGuid(),
                    };

                    testCases.Add(testCase);
                }
            }

            return testCases;
        }

        private static Step ConvertStep(TestRailsXmlStep testRailStep)
        {
            var step = new Step
            {
                Action = testRailStep.Action ?? string.Empty,
                Expected = testRailStep.Expected ?? string.Empty,
                TestData = testRailStep.TestData ?? string.Empty,
                SharedStepId = Guid.TryParse(testRailStep.SharedStepId, out var guid) ? guid : null
            };

            if (step.SharedStepId != null)
            {
                _sharedStepsIds.Add(step.SharedStepId.Value);
            }

            return step;
        }

        private static Step ConvertTestCaseToSharedStep(TestRailsXmlStep testRailStep)
        {
            var step = new Step
            {
                Action = testRailStep.Action ?? string.Empty,
                Expected = testRailStep.Expected ?? string.Empty,
                TestData = testRailStep.TestData ?? string.Empty,
                SharedStepId = Guid.TryParse(testRailStep.SharedStepId, out var guid) ? guid : null
            };

            if (step.SharedStepId != null)
            {
                _sharedStepsIds.Add(step.SharedStepId.Value);
            }

            return step;
        }

        private static Step ConvertStep(string? conditionStep)
        {
            var step = new Step
            {
                Action = conditionStep ?? string.Empty,
            };

            return step;
        }

        private static SharedStep? ConvertTestCaseToSharedStep(TestCase? testCase)
        {
            if (testCase == null)
            {
                return null;
            }

            var sharedStep = new SharedStep
            {
                Id = testCase.Id,
                Description = testCase.Description,
                State = testCase.State,
                Priority = testCase.Priority,
                Steps = testCase.Steps,
                Attributes = testCase.Attributes,
                Links = testCase.Links,
                Name = testCase.Name,
                SectionId = testCase.SectionId,
                Tags = testCase.Tags,
                Attachments = testCase.Attachments
            };

            return sharedStep;
        }
    }
}
