namespace ZephyrScaleServerExporter.BatchMerging;

/// <summary>
/// Defines the contract for a service responsible for handling file system operations
/// required during the batch merging process, such as copying directory contents
/// and updating file references.
/// </summary>
public interface IFileProcessor
{
    /// <summary>
    /// Scans specific JSON files within the merged directory and updates internal references
    /// (like Section IDs and Attribute IDs) based on provided mapping dictionaries.
    /// </summary>
    /// <param name="mergedPath">The absolute path to the 'merged' directory.</param>
    /// <param name="sectionMap">A dictionary mapping duplicate Section IDs to original Section IDs.</param>
    /// <param name="attributeMap">A dictionary mapping duplicate Attribute IDs to original Attribute IDs.</param>
    void UpdateReferencesInMergedFiles(string mergedPath, Dictionary<Guid, Guid> sectionMap, Dictionary<Guid, Guid> attributeMap);

    /// <summary>
    /// Copies the contents (files and subdirectories) from a source batch directory
    /// to the target merged directory, excluding a specified file (typically main.json).
    /// </summary>
    /// <param name="batchSourcePath">The absolute path to the source batch directory.</param>
    /// <param name="mergedPath">The absolute path to the target 'merged' directory.</param>
    /// <param name="fileToExclude">The name of the file to exclude during the copy process.</param>
    void CopyBatchContents(string batchSourcePath, string mergedPath, string fileToExclude);
}