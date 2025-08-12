using Importer.Models;
using Models;

namespace Importer.Services;

public interface IBaseWorkItemService
{
    Task<List<CaseAttribute>> ConvertAttributes(IEnumerable<CaseAttribute> attributes,
        Dictionary<Guid, TmsAttribute> tmsAttributes);

    List<Step> AddAttachmentsToSteps(List<Step> steps, Dictionary<string, Guid> attachments);
}