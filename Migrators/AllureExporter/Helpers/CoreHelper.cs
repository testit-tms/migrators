using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter.Helpers;

internal class CoreHelper(ILogger<CoreHelper> logger) : ICoreHelper
{
    private const int MaxTagLength = 30;
    private const string Ellipsis = "...";
    private const int ReservedLength = 3; // "..."

    public void CutLongTags(TestCase testcase)
    {
        testcase.Tags = ProcessTags(testcase.Tags, testcase.Name, isSharedStep: false);
    }

    public void CutLongTags(SharedStep sharedStep)
    {
        sharedStep.Tags = ProcessTags(sharedStep.Tags, sharedStep.Name, isSharedStep: true);
    }

    private List<string> ProcessTags(List<string> tags, string itemName, bool isSharedStep)
    {
        return tags.Select(tag =>
        {
            if (tag.Length <= MaxTagLength) return tag;
            
            var itemType = isSharedStep ? "shared step" : "test case";
            logger.LogWarning("Tag {Tag} in {ItemType} {ItemName} is longer than {MaxLength} symbols, cutting...",
                tag, itemType, itemName, MaxTagLength);
            
            return tag[..(MaxTagLength - ReservedLength)] + Ellipsis;
        }).ToList();
    }

    public void ExcludeLongTags(TestCase testcase)
    {
        testcase.Tags = ExcludeTags(testcase.Tags, testcase.Name, isSharedStep: false);
    }

    public void ExcludeLongTags(SharedStep sharedStep)
    {
        sharedStep.Tags = ExcludeTags(sharedStep.Tags, sharedStep.Name, isSharedStep: true);
    }

    private List<string> ExcludeTags(List<string> tags, string itemName, bool isSharedStep)
    {
        return tags.Where(tag =>
        {
            if (tag.Length <= MaxTagLength) return true;
            
            var itemType = isSharedStep ? "shared step" : "test case";
            logger.LogWarning("Tag {Tag} in {ItemType} {ItemName} is longer than {MaxLength} symbols, skipping...",
                tag, itemType, itemName, MaxTagLength);
            
            return false;
        }).ToList();
    }
}
