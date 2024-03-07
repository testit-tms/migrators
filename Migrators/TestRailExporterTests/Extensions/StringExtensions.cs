namespace TestRailExporterTests.Extensions
{
    public static class StringExtensions
    {
        public static string ToValidPathName(this string input) => new(input.Where(
            c => !Path.GetInvalidPathChars().Contains(c) && !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
    }
}
