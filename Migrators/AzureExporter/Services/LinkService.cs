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
        var convertedLinks = new List<Link>();

        foreach (var link in links)
        {
            var decodedUrl = HttpUtility.UrlDecode(link.Url);
            var urlParts = decodedUrl.Split('/');
            var project = urlParts[6];
            var suffix = link.Title.Equals("Branch")
                ? $"?version={urlParts[^1]}"
                : $"/commit/{urlParts[^1]}";

            var convertedLink = new Link
            {
                Url = $"{_url}/{_projectName}/_git/{project}{suffix}",
                Title = link.Title
            };

            _logger.LogDebug("Converted link {@OldLink}: {@Link}", link, convertedLink);

            convertedLinks.Add(convertedLink);
        }

        return convertedLinks;
    }
}
