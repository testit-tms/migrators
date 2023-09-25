using AzureExporter.Models;
using Models;

namespace AzureExporter.Services;

public interface ILinkService
{
    List<Link> CovertLinks(IEnumerable<AzureLink> links);
}
