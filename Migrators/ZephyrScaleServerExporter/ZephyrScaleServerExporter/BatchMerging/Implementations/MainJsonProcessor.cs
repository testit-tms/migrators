using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;

namespace ZephyrScaleServerExporter.BatchMerging.Implementations;

internal class MainJsonProcessor(
    ILogger<MainJsonProcessor> logger,
    IFileProcessor fileProcessor)
    : IMainJsonProcessor
{
    
    
    public List<Root> LoadMainJsonFromBatches(IEnumerable<string> batchDirectories, string mergedPath, string mainJsonFile)
    {
         var mainJsonObjects = new List<Root>();
         // batchDirPathAbsolute is the full path from GetDirectories
         foreach (var batchDirPathAbsolute in batchDirectories) 
         {
             logger.LogInformation("Processing batch directory: {BatchDirPath}", batchDirPathAbsolute);

             // Copy files and directories first
             fileProcessor.CopyBatchContents(batchDirPathAbsolute, mergedPath, mainJsonFile);

            // Then attempt to load main.json
            var mainJson = LoadMainJson(batchDirPathAbsolute, mainJsonFile);
            if (mainJson != null)
            {
                mainJsonObjects.Add(mainJson);
            }
         }
         return mainJsonObjects;
    }

    /// <summary>
    /// Loads and deserializes the specified main JSON file (expected to be 'main.json')
    /// from a given batch directory path into a Root object.
    /// Handles potential errors such as the file not existing, JSON deserialization issues,
    /// or other exceptions during file processing.
    /// </summary>
    /// <param name="batchPath">The absolute path to the batch directory.</param>
    /// <param name="mainJsonFile">The name of the main JSON file to load (e.g., "main.json").</param>
    /// <returns>A deserialized Root object if successful, otherwise null.</returns>
    private Root? LoadMainJson(string batchPath, string mainJsonFile)
    {
        var mainJsonPath = Path.Combine(batchPath, mainJsonFile);
        if (!File.Exists(mainJsonPath))
        {
            logger.LogWarning("{MainJsonFile} not found in {BatchPath}. Skipping.", mainJsonFile, batchPath);
            return null;
        }

        try
        {
            var mainJsonContent = File.ReadAllText(mainJsonPath);
            var mainJsonObject = JsonSerializer.Deserialize<Root>(mainJsonContent);
            if (mainJsonObject == null)
            {
                 logger.LogWarning("Could not deserialize {MainJsonFile} from {BatchPath} (result was null).", mainJsonFile, batchPath);
                 return null;
            }
            logger.LogInformation("Successfully loaded {MainJsonFile} from {BatchPath}", mainJsonFile, batchPath);
            return mainJsonObject;
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "JSON deserialization error for {MainJsonPath}", mainJsonPath);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading or processing {MainJsonPath}", mainJsonPath);
            return null;
        }
    }
}