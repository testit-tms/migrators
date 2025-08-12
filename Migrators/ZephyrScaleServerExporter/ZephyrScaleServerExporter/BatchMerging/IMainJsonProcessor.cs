using Models;

namespace ZephyrScaleServerExporter.BatchMerging;

/// <summary>
/// Defines the contract for a service responsible for processing the main JSON ('main.json') files
/// found within batch export directories during the merge operation.
/// </summary>
public interface IMainJsonProcessor
{
    /// <summary>
    /// Iterates through a collection of batch directory paths, orchestrates the copying
    /// of their contents (via IFileProcessor), and loads the main JSON file from each.
    /// </summary>
    /// <param name="batchDirectories">An enumerable collection of absolute paths to the batch directories.</param>
    /// <param name="mergedPath">The absolute path to the target 'merged' directory.</param>
    /// <param name="mainJsonFile">The name of the main JSON file to load from each batch directory.</param>
    /// <returns>A list of successfully loaded and deserialized Root objects.</returns>
    List<Root> LoadMainJsonFromBatches(IEnumerable<string> batchDirectories, string mergedPath, string mainJsonFile);

    // Potentially other methods related to main.json processing if needed in the future.
}