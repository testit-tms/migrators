using Microsoft.Extensions.Logging;

namespace ZephyrScaleServerExporter.BatchMerging.Implementations;

internal class FileProcessor(
    ILogger<App> logger)
    : IFileProcessor
{
    
    public void UpdateReferencesInMergedFiles(string mergedPath, Dictionary<Guid, Guid> sectionMap, Dictionary<Guid, Guid> attributeMap)
    {
        if (sectionMap.Count == 0 && attributeMap.Count == 0)
        {
            logger.LogInformation(
                "No duplicate sections or attributes found, skipping reference updates in individual files.");
            return;
        }

        logger.LogInformation(
            "Starting text-based update of SectionId and AttributeId references in merged files at {MergedPath}...",
            mergedPath);

        // Find relevant JSON files
        var jsonFiles = Directory.GetFiles(mergedPath, "*.json", SearchOption.AllDirectories)
            .ToList();

        int updatedFilesCount = 0;
        int replacementsMadeTotal = 0;

        foreach (var filePath in jsonFiles)
        {
            (updatedFilesCount, replacementsMadeTotal) = HandleFilePath(
                filePath, sectionMap, attributeMap, updatedFilesCount, replacementsMadeTotal);
        }

        logger.LogInformation(
            "Finished text-based reference updates. {FileCount} files updated with a total of {ReplacementCount} replacements.",
            updatedFilesCount, replacementsMadeTotal);
    }

    private (int, int) HandleFilePath(string filePath, 
        Dictionary<Guid, Guid> sectionMap,  
        Dictionary<Guid, Guid> attributeMap, int updatedFilesCount,
        int replacementsMadeTotal)
    {
        bool fileModified = false;
        int replacementsInFile = 0;
        try
        {
            string originalContent = File.ReadAllText(filePath);
            string currentContent = originalContent;

            ( var sectionsModified, replacementsInFile, currentContent ) = 
                ReplaceSectionIds(filePath, sectionMap, replacementsInFile, currentContent);
            
            ( var attributesModified, replacementsInFile, currentContent ) = 
                ReplaceAttributeIds(filePath, attributeMap, replacementsInFile, currentContent);

            fileModified = sectionsModified || attributesModified;
            
            // Write back only if changes were made
            if (fileModified)
            {
                File.WriteAllText(filePath, currentContent);
                updatedFilesCount++;
                replacementsMadeTotal += replacementsInFile;
            }
        }
        catch (IOException ioEx)
        {
            logger.LogError(ioEx, "Failed to read/write file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while processing file: {FilePath}", filePath);
        }

        return (updatedFilesCount, replacementsMadeTotal);
    }

    /// <summary>
    /// Replaces duplicate Section IDs with original ones in JSON file content.
    /// Searches for all occurrences of duplicate section GUIDs in quotes and replaces them with original GUIDs.
    /// </summary>
    /// <param name="filePath">Path to the file being processed (used for logging only)</param>
    /// <param name="sectionMap">Mapping dictionary: key - duplicate section GUID, value - original GUID</param>
    /// <param name="replacementsInFile">Current number of replacements in the file</param>
    /// <param name="currentContent">File content to process</param>
    /// <returns>Tuple: (file was modified, total replacement count, updated file content)</returns>
    private (bool, int, string) ReplaceSectionIds(string filePath, 
        Dictionary<Guid, Guid> sectionMap,  
        int replacementsInFile,
        string currentContent)
    {
        bool fileModified = false;
        
        foreach (var kvp in sectionMap)
        {
            string duplicateIdString = $"\"{kvp.Key}\""; // e.g., "\"abc-123-def\""
            string originalIdString = $"\"{kvp.Value}\"";  // e.g., "\"xyz-456-ghi\""

            if (currentContent.Contains(duplicateIdString))
            {
                string tempContent = currentContent.Replace(duplicateIdString, originalIdString);
                if (tempContent != currentContent)
                {
                    replacementsInFile++;
                    currentContent = tempContent;
                    fileModified = true;
                    logger.LogTrace("Replaced section ID {DuplicateId} with {OriginalId} in {FilePath}",
                        kvp.Key, kvp.Value, filePath);
                }
            }
        }
        
        return (fileModified, replacementsInFile, currentContent);
    }

    /// <summary>
    /// Replaces duplicate Attribute IDs with original ones in JSON file content.
    /// Specifically searches for "id" field in attribute arrays and replaces duplicate GUIDs with original ones.
    /// Uses more precise search with "id": "guid" pattern for safety.
    /// </summary>
    /// <param name="filePath">Path to the file being processed (used for logging only)</param>
    /// <param name="attributeMap">Mapping dictionary: key - duplicate attribute GUID, value - original GUID</param>
    /// <param name="replacementsInFile">Current number of replacements in the file</param>
    /// <param name="currentContent">File content to process</param>
    /// <returns>Tuple: (file was modified, total replacement count, updated file content)</returns>
    private (bool, int, string) ReplaceAttributeIds(string filePath, 
        Dictionary<Guid, Guid> attributeMap,  
        int replacementsInFile,
        string currentContent)
    {
        bool fileModified = false;
        
        foreach (var kvp in attributeMap)
        {
            // Target specifically the "id" field in attribute arrays for safety
            string duplicateIdString = $"\"id\": \"{kvp.Key}\""; // e.g., "\"id\": \"abc-123-def\""
            string originalIdString = $"\"id\": \"{kvp.Value}\""; // e.g., "\"id\": \"xyz-456-ghi\""

            if (currentContent.Contains(duplicateIdString))
            {
                string tempContent = currentContent.Replace(duplicateIdString, originalIdString);
                if (tempContent != currentContent)
                {
                    replacementsInFile++;
                    currentContent = tempContent;
                    fileModified = true;
                    logger.LogTrace("Replaced attribute ID {DuplicateId} with {OriginalId} in {FilePath}",
                        kvp.Key, kvp.Value, filePath);
                }
            }
        }
        
        return (fileModified, replacementsInFile, currentContent);
    }

    
    public void CopyBatchContents(string batchSourcePath, string mergedPath, string fileToExclude)
    {
        // Copy files (excluding main.json)
        CopyBatchFiles(batchSourcePath, mergedPath, fileToExclude);
        // Copy subdirectories
        CopyBatchSubdirectories(batchSourcePath, mergedPath);
    }

    private void CopyBatchFiles(string batchSourcePath, string mergedPath, string mainJsonFile)
    {
        try
        {
            foreach (var file in Directory.GetFiles(batchSourcePath))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.Equals(mainJsonFile, StringComparison.OrdinalIgnoreCase)) continue;
                var destFilePath = Path.Combine(mergedPath, fileName);
                try
                {
                    File.Copy(file, destFilePath, true); // Allow overwrite
                    logger.LogTrace("Copied file {FileName} to merged directory.", fileName);
                }
                catch (IOException ioEx)
                {
                    // Log specific file copy errors but continue with others
                    logger.LogWarning(ioEx, "Could not copy file {SourceFile} to {DestinationFile}. Skipping.",
                        file, destFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enumerating or copying files from batch directory {BatchSourcePath}",
                batchSourcePath);
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
            Environment.Exit(1);
        }
    }

    private void CopyBatchSubdirectories(string batchSourcePath, string mergedPath)
    {
        try
        {
            foreach (var subDir in Directory.GetDirectories(batchSourcePath))
            {
                var subDirName = Path.GetFileName(subDir);
                var destSubDirPath = Path.Combine(mergedPath, subDirName);
                try
                {
                    Utils.CopyDirectory(subDir,
                        destSubDirPath); // Assumes Utils.CopyDirectory exists and handles recursion/errors
                    logger.LogTrace("Copied subdirectory {SubDirName} to merged directory.", subDirName);
                }
                catch (Exception copyEx)
                {
                    // Log specific directory copy errors but continue
                    logger.LogWarning(copyEx, "Could not copy subdirectory {SourceDir} to {DestinationDir}. Skipping.",
                        subDir, destSubDirPath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enumerating or copying subdirectories from batch directory {BatchSourcePath}",
                batchSourcePath);
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
            Environment.Exit(1);
        }
    }
}