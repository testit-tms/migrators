using Models;
using Newtonsoft.Json;
using TestRailImporter.Models;
using Attribute = Models.Attribute;
using Section = Models.Section;

namespace TestRailImporter.Services
{
    public class TestRailBinder
    {
        private readonly List<Guid> _testCases = new();

        public Root ConvertTestRailXmlSuite(TestRailsXmlSuite testRailsXmlSuite, List<CustomAttributeModel> customAttributes)
        {
            StoreTestCases(testRailsXmlSuite.Sections);

            var root = new Root
            {
                ProjectName = testRailsXmlSuite.Name ?? string.Empty,
                Attributes = ConvertAttributes(customAttributes),
                Sections = ConvertSections(testRailsXmlSuite.Sections),
                TestCases = _testCases,
                SharedSteps = new List<Guid>()
            };

            return root;
        }

        private List<Attribute> ConvertAttributes(IEnumerable<CustomAttributeModel> customAttributes)
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

        private List<Section> ConvertSections(TestRailsXmlSection[]? testRailSections)
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

        private void StoreTestCases(TestRailsXmlSection[]? testRailSections)
        {
            if (testRailSections == null)
            {
                return;
            }

            foreach (var section in testRailSections)
            {
                _testCases.AddRange(section.Cases?.Select(testCase => Guid.TryParse(testCase.Id, out var guid) ? guid : Guid.NewGuid()) ?? new List<Guid>());
                StoreTestCases(section.Sections);
            }
        }
    }
}
