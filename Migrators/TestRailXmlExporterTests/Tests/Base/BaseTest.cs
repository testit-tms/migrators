using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Text;
using TestRailXmlExporterTests.Extensions;

namespace TestRailXmlExporterTests.Tests.Base;

[TestFixture]
public abstract class BaseTest
{
    private protected static readonly string inputDirectory;
    private protected static readonly string outputDirectory;

    static BaseTest()
    {
        var dataDirectory = Path.Combine(
            Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName!,
            "Data"
        );
        inputDirectory = Path.Combine(dataDirectory, "Input");
        outputDirectory = Path.Combine(dataDirectory, "Output");
    }

    private protected static async Task AssertOrUpdateExpectedJsonAsync<T>(T actualModel) where T : notnull
    {
        var expectedPath = Path.Combine(outputDirectory, TestContext.CurrentContext.Test.Name.ToValidPathName());
        Directory.CreateDirectory(expectedPath);
        var expectedJson = Path.Combine(expectedPath, $"{typeof(T).Name}.json");

        if (File.Exists(expectedJson))
        {
            var expectedModel = await DeserializeFileAsync<T>(expectedJson).ConfigureAwait(false);

            actualModel.Should().BeEquivalentTo(
                expectedModel,
                options => options.Excluding(memberInfo =>
                    memberInfo.Path.EndsWith("Id") ||
                    memberInfo.Path.EndsWith("TestCases")
                )
            );
        }
        else
        {
            var actualText = JsonConvert.SerializeObject(actualModel, Formatting.Indented);
            await File.WriteAllTextAsync(expectedJson, actualText, Encoding.UTF8).ConfigureAwait(false);
        }
    }

    private protected static async Task<T> DeserializeFileAsync<T>(string file) where T : notnull
    {
        var text = await File.ReadAllTextAsync(file, Encoding.UTF8).ConfigureAwait(false);
        var model = JsonConvert.DeserializeObject<T>(text)!;

        return model;
    }
}
