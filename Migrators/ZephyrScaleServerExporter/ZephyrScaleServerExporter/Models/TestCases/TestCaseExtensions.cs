namespace ZephyrScaleServerExporter.Models.TestCases;

public static class TestCaseExtensions
{
    private static List<string> ToStrings(this List<IssueLink> issueLinks)
    {
        return issueLinks.Select(x => x.IssueId).ToList();
    }


    private static ZephyrTestCase ToTestCase(this ZephyrTestCaseRoot tc)
    {
        return new ZephyrTestCase
        {
            JiraId = tc.Id.ToString(),
            Key = tc.Key,
            Name = tc.Name,
            Description = tc.Description,
            Precondition = tc.Precondition,
            Labels = tc.Labels,
            Priority = tc.Priority.Name,
            Status = tc.Status.Name,
            IssueLinks = tc.IssueLinks?.ToStrings() ?? [],
            TestScript = tc.TestScript,
            CustomFields = tc.CustomFields,
            Folder = tc.Folder?.Name,
            Component = tc.Component,
            OwnerKey = tc.OwnerKey,
            Parameters = new Dictionary<string, object>()
        };
    }

    public static List<ZephyrTestCase> ToTestCases(this List<ZephyrTestCaseRoot> testCasesRoots)
    {
        return testCasesRoots
            .Select(ToTestCase)
            .ToList();
    }

}
