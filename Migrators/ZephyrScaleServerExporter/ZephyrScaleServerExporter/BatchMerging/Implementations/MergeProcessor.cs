using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Services;

namespace ZephyrScaleServerExporter.BatchMerging.Implementations;

internal class MergeProcessor(
    IDetailedLogService detailedLogService,
    ILogger<MergeProcessor> logger,
    IFileProcessor fileProcessor,
    IMainJsonProcessor mainJsonProcessor,
    IOptions<AppConfig> config)
    : IMergeProcessor
{
    private readonly AppConfig _config = config.Value;
    private const string MergedDirName = "merged";
    private const string BatchDirPrefix = "batch_";
    private const string MainJsonFile = "main.json";

    // Temporary storage for maps - better to return them from MergeMainJsonObjects
    private Dictionary<Guid, Guid> _duplicateSectionMap = new();
    private Dictionary<Guid, Guid> _duplicateAttributeMap = new();


    private readonly JsonSerializerOptions _defaultOptions = new ()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };
    
    public void MergeProjects()
    {
        var projectPath = Path.Combine(_config.ResultPath, _config.Zephyr.ProjectKey);
        logger.LogInformation("Starting merge process for project path: {ProjectPath}", projectPath);

        var batchDirectories = Utils.ValidateAndGetBatchDirectories(projectPath, BatchDirPrefix, logger);
        if (batchDirectories == null || batchDirectories.Count == 0)
        {
            return;
        }

        var mergedPath = Utils.InitializeMergedDirectory(projectPath, MergedDirName, logger);
        if (string.IsNullOrEmpty(mergedPath))
        {
            return;
        }

        var mainJsonObjects =
            mainJsonProcessor.LoadMainJsonFromBatches(batchDirectories, mergedPath, MainJsonFile);

        if (mainJsonObjects.Count == 0)
        {
            logger.LogWarning(
                "No valid main.json files were loaded from any batch directory in {ProjectPath}. Cannot proceed with merge.",
                projectPath);
            return;
        }

        // Merge the main.json objects
        var mergedMainJsonObject = MergeMainJsonObjects(mainJsonObjects);

        // Save the final merged main.json
        SaveMergedJson(mergedPath, mergedMainJsonObject);

        fileProcessor.UpdateReferencesInMergedFiles(mergedPath, _duplicateSectionMap, _duplicateAttributeMap);

        logger.LogInformation("Merge process completed successfully for project path: {ProjectPath}", projectPath);
    }


    /// <summary>
    /// Serializes the provided merged Root object into a JSON string with proper indentation and Unicode encoding.
    /// Writes the resulting JSON string to the main JSON file (e.g., "main.json") within the specified merged directory path.
    /// Logs the outcome and throws an IOException if any error occurs during serialization or file writing.
    /// </summary>
    /// <param name="mergedPath">The absolute path to the 'merged' directory where the output file will be saved.</param>
    /// <param name="mergedData">The final merged Root object to be saved.</param>
    /// <exception cref="IOException">Thrown if serialization or file writing fails.</exception>
    private void SaveMergedJson(string mergedPath, Root mergedData)
    {
        var mergedMainJsonPath = Path.Combine(mergedPath, MainJsonFile);
        try
        {
            var mergedJson = JsonSerializer.Serialize(mergedData, _defaultOptions);
            File.WriteAllText(mergedMainJsonPath, mergedJson);
            logger.LogInformation("Saved merged {MainJsonFile} to {MergedPath}", MainJsonFile, mergedMainJsonPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving merged {MainJsonFile} to {MergedPath}", MainJsonFile, mergedMainJsonPath);
            throw new IOException($"Failed to save merged JSON to {mergedMainJsonPath}", ex);
        }
    }


    /// <summary>
    /// Merges a list of Root objects (representing multiple main.json files) into a single Root object.
    /// It uses the first Root object in the list as the base for ProjectName and Attributes.
    /// Sections are merged recursively, handling duplicates based on name, and a map of duplicate section IDs is generated.
    /// Attribute definitions are merged based on unique names, and a map of duplicate attribute IDs is generated.
    /// SharedSteps and TestCases lists are merged, ensuring uniqueness based on their IDs.
    /// The resulting duplicate maps are stored in private class fields (_duplicateSectionMap, _duplicateAttributeMap) for later use.
    /// </summary>
    /// <param name="mainJsonObjectList">A list of Root objects loaded from individual batch main.json files.</param>
    /// <returns>A single Root object representing the merged data.</returns>
    private Root MergeMainJsonObjects(List<Root> mainJsonObjectList)
    {
        var firstRoot = mainJsonObjectList[0];
        var mergedRoot = new Root
        {
            ProjectName = firstRoot.ProjectName,
            Attributes = firstRoot.Attributes,
            Sections = [],
            SharedSteps = [],
            TestCases = []
        };

        logger.LogInformation("Starting merge logic for {Count} main.json objects...", mainJsonObjectList.Count);

        var (duplicateSectionMap, duplicateAttributeMap) 
            = ProcessMainJsonMerge(mainJsonObjectList, mergedRoot);
        
        _duplicateSectionMap = duplicateSectionMap;
        _duplicateAttributeMap = duplicateAttributeMap;


        logger.LogInformation("Merge logic complete. Merged object created.");
        return mergedRoot;
    }

    private ( Dictionary<Guid, Guid>, Dictionary<Guid, Guid> )  ProcessMainJsonMerge(List<Root> mainJsonObjectList,
        Root mergedRoot)
    {
        // Store the actual Section object for uniqueness check
        var uniqueSectionsByName = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase); 
        var duplicateSectionMap = new Dictionary<Guid, Guid>();

        var uniqueAttributeNames = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var duplicateAttributeMap = new Dictionary<Guid, Guid>();

        var uniqueSharedStepIds = new HashSet<Guid>();
        var uniqueTestCaseIds = new HashSet<Guid>();
        
        foreach (var rootObject in mainJsonObjectList)
        {
            ProcessSectionsRecursively(rootObject.Sections, mergedRoot.Sections, uniqueSectionsByName,
                duplicateSectionMap);

            foreach (var stepId in rootObject.SharedSteps)
            {
                if (uniqueSharedStepIds.Add(stepId)) mergedRoot.SharedSteps.Add(stepId);
            }

            foreach (var testCaseId in rootObject.TestCases)
            {
                if (uniqueTestCaseIds.Add(testCaseId)) mergedRoot.TestCases.Add(testCaseId);
            }

            ProcessAttributes(rootObject, uniqueAttributeNames, duplicateAttributeMap, mergedRoot);
        }

        return (duplicateSectionMap, duplicateAttributeMap);
    }

    private static void ProcessAttributes(Root rootObject,
        Dictionary<string, Guid> uniqueAttributeNames,
        Dictionary<Guid, Guid> duplicateAttributeMap,
        Root mergedRoot)
    {
        // Add unique attributes definitions based on name
        foreach (var attr in rootObject.Attributes)
        {
            if (uniqueAttributeNames.TryGetValue(attr.Name, out var originalAttributeId))
            {
                if (attr.Id != originalAttributeId)
                {
                    duplicateAttributeMap[attr.Id] = originalAttributeId;
                    
                }
            }
            else
            {
                uniqueAttributeNames.Add(attr.Name, attr.Id);
                // Add the unique attribute definition to the merged list
                if (mergedRoot.Attributes.TrueForAll(a => a.Id != attr.Id))
                {
                    mergedRoot.Attributes.Add(attr);
                }
            }
        }
    }


    /// <summary>
    /// Recursively processes a list of sections to merge them into a target list,
    /// handling duplicates based on section names (case-insensitive).
    /// Unique sections (based on the first occurrence of a name) are added to the target list.
    /// Subsequent sections with the same name are considered duplicates, and their IDs are mapped
    /// to the ID of the original section in the duplicateSectionMap.
    /// The children of both unique and duplicate sections are processed recursively.
    /// </summary>
    /// <param name="sectionsToProcess">The collection of sections to process in the current recursive step.</param>
    /// <param name="targetMergedList">The list where unique sections from this level should be added.</param>
    /// <param name="uniqueSectionsByName">A dictionary tracking already encountered section names and the reference to their first occurrence Section object.</param>
    /// <param name="duplicateSectionMap">A dictionary where mappings from duplicate section IDs (key) to original section IDs (value) are stored.</param>
    private void ProcessSectionsRecursively(
        IEnumerable<Section>? sectionsToProcess,
        List<Section> targetMergedList,
        Dictionary<string, Section> uniqueSectionsByName,
        Dictionary<Guid, Guid> duplicateSectionMap)
    {
        if (sectionsToProcess == null) return; // Guard against null list

        foreach (var currentSection in sectionsToProcess)
        {
            // Try to get the original Section object directly
            if (uniqueSectionsByName.TryGetValue(currentSection.Name, out var originalSection))
            {
                var originalSectionId = originalSection.Id; // Get ID from the found object
                if (currentSection.Id != originalSectionId)
                {
                    duplicateSectionMap[currentSection.Id] = originalSectionId;
                    detailedLogService.LogDebug(
                        "Mapping duplicate section '{Name}' (ID: {DuplicateId}) to original (ID: {OriginalId})",
                        currentSection.Name, currentSection.Id, originalSectionId);
                        
                    // Process children of duplicate, merging them directly into the original section's children list
                    // No need to search targetMergedList anymore
                    ProcessSectionsRecursively(currentSection.Sections, originalSection.Sections, uniqueSectionsByName,
                        duplicateSectionMap); 
                }
                // If Id is same, it's the original one we already added, just process its children if any.
                else if (currentSection.Sections.Count != 0)
                {
                    // Children should be processed into the originalSection's list
                    // The 'originalSection' IS the section in targetMergedList (or nested within one)
                     ProcessSectionsRecursively(currentSection.Sections, originalSection.Sections,
                           uniqueSectionsByName, duplicateSectionMap);
                }
            }
            else
            {
                // Section name is unique, create and add it
                var newMergedSection = new Section
                {
                    Id = currentSection.Id,
                    Name = currentSection.Name,
                    PreconditionSteps = [..currentSection.PreconditionSteps],
                    PostconditionSteps = [..currentSection.PostconditionSteps],
                    Sections = [] // Initialize children list
                };
                targetMergedList.Add(newMergedSection); // Add to the current target list
                uniqueSectionsByName.Add(currentSection.Name, newMergedSection); // Add the new object to the map

                detailedLogService.LogDebug("Added unique section '{Name}' (ID: {Id})", newMergedSection.Name, newMergedSection.Id);
                
                // Process children for the newly added section
                ProcessSectionsRecursively(currentSection.Sections, newMergedSection.Sections, uniqueSectionsByName,
                    duplicateSectionMap);
            }
        }
    }
}