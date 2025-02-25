using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;
using System.Text;
using System.Xml.Linq;
using TestRailXmlExporter.Models;
using Attribute = Models.Attribute;
using Section = Models.Section;

namespace TestRailXmlExporter.Services
{
    public class ExportService
    {
        private const int DEFAULT_DURATION_IN_SEC = 60 * 5;
        private const int MAX_TAG_NAME_LENGTH = 30;

        private readonly List<Section> _sectionsData;
        private readonly List<Guid> _sharedStepsIds;
        private readonly List<TestCase> _testCasesData;

        private readonly ILogger<ExportService> _logger;
        private readonly IWriteService _writeService;

        public ExportService(ILogger<ExportService> logger, IWriteService writeService)
        {
            _logger = logger;
            _writeService = writeService;

            _sectionsData = new();
            _sharedStepsIds = new();
            _testCasesData = new();
        }

        public async Task ExportProjectAsync(TestRailsXmlSuite testRailsXmlSuite, List<CustomAttributeModel> customAttributes)
        {
            _logger.LogInformation("Exporting project");

            var attributeData = ConvertAttributes(customAttributes).Distinct().ToList();
            ConvertSectionsWithTestCases(testRailsXmlSuite.Sections, attributeData, testRailsXmlSuite.Name);

            foreach (var sharedStepId in _sharedStepsIds)
            {
                var testCase = _testCasesData.FirstOrDefault(testCase => _sharedStepsIds.Contains(testCase.Id));
                var sharedStep = ConvertTestCaseToSharedStep(testCase);

                if (sharedStep == null)
                {
                    continue;
                }

                await _writeService.WriteSharedStep(sharedStep).ConfigureAwait(false);
            }

            foreach (var testCase in _testCasesData)
            {
                await _writeService.WriteTestCase(testCase).ConfigureAwait(false);
            }

            var root = new Root
            {
                ProjectName = testRailsXmlSuite.Name ?? string.Empty,
                Attributes = attributeData.OrderBy(attribute => attribute.Name).ToList(),
                Sections = _sectionsData,
                SharedSteps = _sharedStepsIds,
                TestCases = _testCasesData.Select(testCase => testCase.Id).ToList()
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
                    Options = customAttribute.Options?.Select(option => option.Value ?? string.Empty).ToList()
                        ?? new List<string>()
                };

                return attribute;
            });


            return attributes.ToList();
        }

        private List<Section> ConvertSectionsWithTestCases(IEnumerable<TestRailsXmlSection>? testRailSections,
            List<Attribute> customAttributes, string? xmlSuiteName, bool hasRootSection = false)
        {
            var sections = new List<Section>();

            if (testRailSections == null || !testRailSections.Any())
            {
                return sections;
            }

            foreach (var testRailSection in testRailSections)
            {
                Section section = new()
                {
                    Id = Guid.NewGuid(),
                    Name = testRailSection.Name ?? string.Empty,
                    Sections = ConvertSectionsWithTestCases(testRailSection.Sections, customAttributes, xmlSuiteName, true),
                    PreconditionSteps = new List<Step>(),
                    PostconditionSteps = new List<Step>()
                };

                if (hasRootSection)
                {
                    sections.Add(section);
                }
                else
                {
                    _sectionsData.Add(section);
                }

                if (testRailSection.Cases == null || testRailSection.Cases.Length == 0)
                {
                    continue;
                }

                foreach (var testRailCase in testRailSection.Cases)
                {
                    var test = testRailCase.Custom?.CustomAttributes?.SingleOrDefault(attribute => attribute.Name == "comment");
                    var testCase = new TestCase
                    {
                        Id = Guid.TryParse(testRailCase.Id, out var guid) ? guid : Guid.NewGuid(),
                        Description = ExtractDescription(testRailCase),
                        State = Enum.TryParse(testRailCase.State, out StateType type) ? type : StateType.NeedsWork,
                        Priority = Enum.TryParse(testRailCase.Priority, out PriorityType priority) ? priority
                            : PriorityType.Medium,
                        Steps = ConvertSteps(testRailCase.Custom.GetValueOrDefault(new TestRailsXmlCaseData())),
                        PreconditionSteps = ExtractPreconditions(testRailCase),
                        PostconditionSteps = new List<Step>(),
                        Duration = (int.TryParse(testRailCase.Estimate, out var duration) ? duration
                            : DEFAULT_DURATION_IN_SEC) * 1000,
                        Attributes = GetTestCaseAttributes(testRailCase, customAttributes),
                        Tags = new List<string>() { new((xmlSuiteName ?? string.Empty).Take(MAX_TAG_NAME_LENGTH).ToArray()) },
                        Attachments = new List<string>(),
                        Iterations = new List<Iteration>(),
                        Links = new List<Link>(),
                        Name = testRailCase.Title ?? string.Empty,
                        SectionId = section.Id,
                    };

                    _testCasesData.Add(testCase);
                }
            }

            return sections;
        }

        private List<Step> ConvertSteps(TestRailsXmlCaseData xmlCaseData)
        {
            var steps = new List<Step>();

            foreach (var xmlStep in xmlCaseData.Steps)
            {
                var step = new Step
                {
                    Action = FormatStepText(xmlStep.Action),
                    Expected = FormatStepText(xmlStep.Expected),
                    TestData = FormatStepText(xmlStep.TestData),
                    SharedStepId = Guid.TryParse(xmlStep.SharedStepId, out var guid) ? guid : null
                };

                if (step.SharedStepId != null)
                {
                    _sharedStepsIds.Add(step.SharedStepId.Value);
                }

                steps.Add(step);
            }

            if (!string.IsNullOrWhiteSpace(xmlCaseData.Step) || !string.IsNullOrWhiteSpace(xmlCaseData.Expected))
            {
                steps.Insert(0, new Step()
                {

                    Action = FormatStepText(xmlCaseData.Step),
                    Expected = FormatStepText(xmlCaseData.Expected),
                });
            }

            return steps;
        }

        private static string ExtractDescription(TestRailsXmlCase testRailCase)
        {
            var stringBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(testRailCase.Custom?.Comments))
            {
                stringBuilder.AppendLine(testRailCase.Custom?.Comments).AppendLine();
            }

            stringBuilder.Append($"Imported from {testRailCase.Id}");

            return stringBuilder.ToString();
        }

        private static List<Step> ExtractPreconditions(TestRailsXmlCase testRailCase)
        {
            var action = FormatStepText(testRailCase.Custom.GetValueOrDefault(new TestRailsXmlCaseData()).Preconditions);

            if (string.IsNullOrWhiteSpace(action))
            {
                return new List<Step>();
            }
            else
            {
                var step = new Step
                {
                    Action = action,
                };

                return new List<Step>() { step };
            }
        }

        private static List<CaseAttribute> GetTestCaseAttributes(TestRailsXmlCase testRailCase,
            List<Attribute> customAttributes)
        {
            var testCaseAttributes = testRailCase.Custom?.CustomAttributes?.Select(attribute =>
            {
                return new CaseAttribute()
                {
                    Id = customAttributes.FirstOrDefault(customAttribute =>
                        customAttribute.Name == attribute.Name)?.Id ?? Guid.NewGuid(),
                    Value = XElement.Parse(attribute?.OuterXml ?? string.Empty)?.Element("value")?.Value ?? string.Empty
                };
            }).ToList() ?? new List<CaseAttribute>();

            var attributesNames = new string[]
            {
                nameof(TestRailsXmlCase.References),
                nameof(TestRailsXmlCase.Type)
            };

            foreach (var attributeName in attributesNames)
            {
                var attribute = customAttributes.SingleOrDefault(attribute =>
                    attribute?.Name == attributeName, null);

                if (attribute == null)
                {
                    continue;
                }

                testCaseAttributes.Add(new CaseAttribute()
                {
                    Id = attribute.Id,
                    Value = testRailCase.GetType().GetProperty(attributeName)?.GetValue(testRailCase, null) as string
                        ?? string.Empty
                });
            }

            testCaseAttributes.Sort((first, second) => string.Compare(
                customAttributes.SingleOrDefault(attribute => attribute?.Id == first.Id, null)?.Name,
                customAttributes.SingleOrDefault(attribute => attribute?.Id == second.Id, null)?.Name));

            return testCaseAttributes;
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

        private static string FormatStepText(string? input)
        {
            return string.IsNullOrWhiteSpace(input) ? string.Empty : input.Replace("\n", "\n<br>\n");
        }
    }
}
