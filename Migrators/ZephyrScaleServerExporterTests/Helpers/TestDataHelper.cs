using NUnit.Framework.Internal;

namespace ZephyrScaleServerExporterTests.Helpers;

public static class TestDataHelper
{
    private static readonly Randomizer Randomizer = Randomizer.CreateRandomizer();

    public static int GenerateProjectId(int min = 10000, int max = 99999)
    {
        return Randomizer.Next(min, max);
    }
}
