using Microsoft.Extensions.Logging;
using Models;
using ZephyrScaleServerExporter.Client;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Services.TestCase.Implementations;

public class TestCaseAdditionalLinksService(
    IDetailedLogService detailedLogService,
    ILogger<TestCaseConvertService> logger,
    IClient client)
    : ITestCaseAdditionalLinksService
{

    public async Task<List<Link>> GetAdditionalLinks(ZephyrTestCase zephyrTestCase)
    {
        try
        {
            detailedLogService.LogDebug("Getting additional links for {Key}...", zephyrTestCase.Key);
            var res = await client.GetTestCaseTracesV2(
                zephyrTestCase.Key!, zephyrTestCase.IsArchived);
            // otherwise = never?
            var tlRoot = res?.Results.FirstOrDefault() ?? await client.GetTestCaseTraces(zephyrTestCase.Key!);

            if (tlRoot != null && tlRoot.Id.ToString() != zephyrTestCase.JiraId)
            {
                zephyrTestCase.JiraId = tlRoot.Id.ToString();
            }

            if (tlRoot?.TraceLinks == null) {
                return [];
            }
            var links = ConvertWebLinksByPage(tlRoot.TraceLinks);
            links.AddRange(await ConvertConfluenceLinksByPage(tlRoot.TraceLinks));
            links.AddRange(await ConvertIssueLinksByPage(tlRoot.TraceLinks));
            return links;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Getting additional links failed.");
            return [];
        }

    }

    private List<Link> ConvertWebLinksByPage(List<TraceLink> traceLinks)
    {
        logger.LogInformation("Converting web links for test case");

        var webLinks = traceLinks
            .Where(x => x is { UrlDescription: not null, Url: not null }).ToList();

        var newLinks = webLinks.Select(webLink =>
            new Link { Title = webLink.UrlDescription!, Url = webLink.Url! }
        ).ToList();

        logger.LogInformation("Converted web links: {@Links}", newLinks);

        return newLinks;
    }

    private async Task<List<Link>> ConvertConfluenceLinksByPage(List<TraceLink> traceLinks)
    {
        var confluenceTraceLinks = traceLinks.Where(x => x.ConfluencePageId != null).ToList();

        var newLinks = new List<Link>();

        foreach (var confluenceTraceLink in confluenceTraceLinks)
        {
            try
            {
                var confluenceLinks = await client
                    .GetConfluenceLinksByConfluencePageId(confluenceTraceLink.ConfluencePageId!);
                foreach (var confluenceLink in confluenceLinks)
                {
                    newLinks.Add(
                        new Link
                        {
                            Title = confluenceLink!.Title!,
                            Url = confluenceLink!.Url!
                        }
                    );
                }
            }
            catch (Exception)
            {
                // do nothing if we have confluence page id parsing here
            }
        }

        logger.LogInformation("Converted confluence links: {@Links}", newLinks);

        return newLinks;
    }

    private async Task<List<Link>> ConvertIssueLinksByPage( List<TraceLink> traceLinks)
    {
        logger.LogInformation("Converting issue links for test case");

        var newLinks = new List<Link>();
        var issueIds = traceLinks.Where(x => x.IssueId != null)
            .Select(x => x.IssueId).ToList();
        var url = client.GetBaseUrl().ToString().TrimEnd('/');

        foreach (var issueId in issueIds)
        {
            var issue = await client.GetIssueById(issueId!);
            var newUrl = url + "/browse/" + issue.Key;

            newLinks.Add(
                new Link
                {
                    Title = issue.Fields.Name,
                    Url = newUrl
                }
            );
        }

        logger.LogInformation("Converted issue links: {@Links}", newLinks);

        return newLinks;
    }


    public async Task<Link> ConvertIssueLinkByIssueId(string issueId)
    {
        logger.LogInformation("Converting issue link");

        var url = client.GetBaseUrl().ToString().TrimEnd('/');

        var issue = await client.GetIssueById(issueId!);
        var newUrl = url + "/browse/" + issue.Key;

        return new Link
        {
            Title = issue.Fields.Name,
            Url = newUrl
        };
    }
}
