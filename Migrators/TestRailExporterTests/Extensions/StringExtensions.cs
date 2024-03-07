namespace TestRailExporterTests.Extensions
{
    public static class StringExtensions
    {
        public static string ToValidPathName(this string input) => new(
            input.Where(symbol =>
                !Path.GetInvalidPathChars().Contains(symbol) &&
                !Path.GetInvalidFileNameChars().Contains(symbol)
            ).ToArray()
        );
    }
}
