using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Text;
using TestRailExporterTests.Extensions;

namespace TestRailExporterTests.Tests.Base;

[TestFixture]
public abstract class BaseTest
{
    private protected static readonly string projectRootPath = Directory.GetParent(Environment.CurrentDirectory)
        ?.Parent
        ?.Parent
        ?.FullName!;

    private protected static async Task AssertOrUpdateExpectedJsonAsync<T>(T actualModel) where T : notnull
    {
        var outputPath = Path.Combine(
            projectRootPath,
            "Data",
            "Output",
            TestContext.CurrentContext.Test.Name.ToValidPathName()
        );

        Directory.CreateDirectory(outputPath);
        var outputJson = Path.Combine(outputPath, $"{typeof(T).Name}.json");

        if (File.Exists(outputJson))
        {
            var expectedModel = DeserializeFile<T>(outputJson);
            actualModel.Should().BeEquivalentTo(expectedModel, options => options.Excluding(x => x.Path.EndsWith("Id")
                || (x.Type == typeof(List<Guid>) && x.Path.EndsWith("TestCases"))));
        }
        else
        {
            await File.WriteAllTextAsync(
                outputJson,
                JsonConvert.SerializeObject(actualModel, Formatting.Indented),
                Encoding.UTF8).ConfigureAwait(false);
        }
    }

    private protected static T DeserializeFile<T>(string file) where T : notnull
    {
        var text = File.ReadAllText(file, Encoding.UTF8);
        var model = JsonConvert.DeserializeObject<T>(text)!;

        return model;
    }
}
