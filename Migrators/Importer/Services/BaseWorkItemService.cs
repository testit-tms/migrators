using Importer.Models;
using Models;

namespace Importer.Services;

public abstract class BaseWorkItemService
{
    private const string OptionsType = "options";

    protected static List<CaseAttribute> ConvertAttributes(IEnumerable<CaseAttribute> attributes,
        Dictionary<Guid, TmsAttribute> tmsAttributes)
    {
        var list = new List<CaseAttribute>();

        foreach (var attribute in attributes)
        {
            var atr = tmsAttributes[attribute.Id];
            var value = string.Equals(atr.Type, OptionsType, StringComparison.InvariantCultureIgnoreCase)
                ? Enumerable.FirstOrDefault(atr.Options, o => o.Value == attribute.Value)?.Id
                    .ToString()
                : attribute.Value;

            list.Add(new CaseAttribute { Id = atr.Id, Value = value });
        }

        return list;
    }
}
