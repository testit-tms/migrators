namespace ZephyrScaleServerExporter.BatchMerging;

/// <summary>
/// Defines the contract for a service responsible for merging the results
/// of multiple batch exports into a single, consolidated output.
/// </summary>
public interface IMergeProcessor
{
    /// <summary>
    /// Orchestrates the entire process of merging data from multiple batch export directories
    /// within a project's result path into a single 'merged' directory.
    /// It validates paths, initializes the target directory, loads data from batch main.json files,
    /// merges this data (handling duplicates), saves the final merged main.json,
    /// and finally updates references in individual test case and shared step files within the merged directory.
    /// </summary>
    public void MergeProjects();
}