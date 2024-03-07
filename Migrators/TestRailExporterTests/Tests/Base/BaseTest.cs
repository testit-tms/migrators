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
            var expectedModel = await DeserializeFileAsync<T>(outputJson).ConfigureAwait(false);

            actualModel.Should().BeEquivalentTo(expectedModel, options => options.Excluding(memberInfo =>
                memberInfo.Path.EndsWith("Id") || memberInfo.Path.EndsWith("TestCases"))
            );
        }
        else
        {
            var actualText = JsonConvert.SerializeObject(actualModel, Formatting.Indented);
            await File.WriteAllTextAsync(outputJson, actualText, Encoding.UTF8).ConfigureAwait(false);
        }
    }

    private protected static async Task<T> DeserializeFileAsync<T>(string file) where T : notnull
    {
        var text = await File.ReadAllTextAsync(file, Encoding.UTF8).ConfigureAwait(false);
        var model = JsonConvert.DeserializeObject<T>(text)!;

        return model;
    }
}
