using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NUnit.Framework.Internal;

namespace TestRailExporterTests.Models;

internal class TestConfiguration : IConfiguration
{
    private static readonly Dictionary<string, string?> _data = new()
    {
        { "resultPath", Path.Combine(Path.GetTempPath(), Randomizer.CreateRandomizer().GetString(5)) },
    };

    public string? this[string key] { get => _data[key]; set => _data[key] = value; }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        throw new NotImplementedException();
    }

    public IChangeToken GetReloadToken()
    {
        throw new NotImplementedException();
    }

    public IConfigurationSection GetSection(string key)
    {
        throw new NotImplementedException();
    }
}
