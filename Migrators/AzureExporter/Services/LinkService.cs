using System.Web;
using AzureExporter.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;

namespace AzureExporter.Services;

public class LinkService : ILinkService
{
    private readonly ILogger<LinkService> _logger;
    private readonly string _projectName;
    private readonly string _url;

    public LinkService(ILogger<LinkService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("azure");
        var url = section["url"];

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Url is not specified");
        }

        _url = url;

        var projectName = section["projectName"];

        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("Project name is not specified");
        }

        _projectName = projectName;
    }

    public List<Link> CovertLinks(IEnumerable<AzureLink> links)
    {
        _logger.LogInformation("Converting links: {@Links}", links);

        var convertedLinks = new List<Link>();

        foreach (var link in links)
        {
            var decodedUrl = HttpUtility.UrlDecode(link.Url);
            while (decodedUrl.Contains('%'))
            {
                decodedUrl = HttpUtility.UrlDecode(decodedUrl);
            }

            var urlParts = decodedUrl.Split('/');

            Link convertedLink;
            if (link.Url.Contains("VersionControl/Changeset"))
            {
                convertedLink = new Link
                {
                    Url = $"{_url}/{_projectName}/_versionControl/changeset/{urlParts.Last()}",
                    Title = link.Title
                };
            }
            else if (link.Url.Contains("VersionControl/VersionedItem"))
            {
                convertedLink = new Link
                {
                    Url =
                        $"{_url}/{_projectName}/_versionControl/?path=${decodedUrl.Split("VersionControl/VersionedItem/$").Last().Replace("changesetVersion", "version").Replace("deletionId=0", "_a=contents")}",
                    Title = link.Title
                };
            }
            else if (link.Url.Contains("VersionControl/LatestItemVersion"))
            {
                continue;
            }
            else
            {
                var project = urlParts[6];
                var suffix = link.Title.Equals("Branch")
                    ? $"?version={urlParts[^1]}"
                    : $"/commit/{urlParts[^1]}";

                convertedLink = new Link
                {
                    Url = $"{_url}/{_projectName}/_git/{project}{suffix}",
                    Title = link.Title
                };
            }

            _logger.LogDebug("Converted link {@OldLink}: {@Link}", link, convertedLink);

            convertedLinks.Add(convertedLink);
        }

        return convertedLinks;
    }
}
