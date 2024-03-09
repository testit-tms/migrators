namespace TestRailExporterTests.Extensions
{
    public static class StringExtensions
    {
        public static string ToValidPathName(this string input)
        {
            var filteredInput = input.Where(symbol =>
                !Path.GetInvalidPathChars().Contains(symbol) &&
                !Path.GetInvalidFileNameChars().Contains(symbol)
            );

            return new(filteredInput.ToArray());
        }
    }
}
