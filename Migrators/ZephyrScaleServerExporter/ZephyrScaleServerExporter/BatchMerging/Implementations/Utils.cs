using Microsoft.Extensions.Logging;

namespace ZephyrScaleServerExporter.BatchMerging.Implementations;

internal static class Utils
{
    
    /// <summary>
    /// Validates the existence of the specified project path and retrieves a list of subdirectory paths
    /// within it that start with the given prefix (case-insensitive).
    /// Logs relevant information, warnings, or errors during the process.
    /// </summary>
    /// <param name="projectPath">The absolute path to the project directory to scan.</param>
    /// <param name="batchDirPrefix">The prefix string that batch directory names should start with.</param>
    /// <param name="logger">The logger instance for logging messages.</param>
    /// <returns>A list of absolute paths to the found batch directories, or null if the project path is invalid,
    /// an error occurs, or no matching directories are found.</returns>
    internal static List<string>? ValidateAndGetBatchDirectories(string projectPath, string batchDirPrefix, ILogger logger)
    {
        if (!Directory.Exists(projectPath))
        {
            logger.LogWarning("Project path {ProjectPath} does not exist. Cannot start merge.", projectPath);
            return null;
        }

        try
        {
            var batchDirectories = Directory.GetDirectories(projectPath)
                .Where(d => Path.GetFileName(d).StartsWith(batchDirPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (batchDirectories.Count == 0)
            {
                logger.LogInformation("No directories starting with '{BatchDirPrefix}' found in {ProjectPath}. Nothing to merge.", batchDirPrefix, projectPath);
                return null; // Or return empty list depending on desired behavior
            }

            logger.LogInformation("Found {Count} batch directories in {ProjectPath}.", batchDirectories.Count, projectPath);
            return batchDirectories;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error accessing batch directories in {ProjectPath}", projectPath);
            return null;
        }
    }
    
    /// <summary>
    /// Recursively copies the contents of a source directory to a destination directory.
    /// Creates the destination directory if it doesn't exist.
    /// Copies all files, overwriting existing ones in the destination.
    /// Recursively copies all subdirectories.
    /// Throws DirectoryNotFoundException if the source directory does not exist.
    /// </summary>
    /// <param name="sourceDir">The absolute path to the source directory.</param>
    /// <param name="destinationDir">The absolute path to the destination directory.</param>
    internal static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }
    
    /// <summary>
    /// Initializes the target directory for merged output within the project path.
    /// It constructs the full path using the provided directory name.
    /// If the directory already exists, it is deleted recursively to ensure a clean state.
    /// Then, the directory is created. Logs the progress and any errors encountered.
    /// </summary>
    /// <param name="projectPath">The absolute path to the project directory.</param>
    /// <param name="mergedDirName">The name of the target directory to initialize (e.g., "merged").</param>
    /// <param name="logger">The logger instance for logging messages.</param>
    /// <returns>The absolute path to the newly created (or cleaned and created) merged directory,
    /// or null if an error occurs during directory operations.</returns>
    internal static string? InitializeMergedDirectory(string projectPath, string mergedDirName, ILogger logger)
    {
        var mergedPath = Path.Combine(projectPath, mergedDirName);
        try
        {
            if (Directory.Exists(mergedPath))
            {
                logger.LogInformation("Cleaning existing merged directory: {MergedPath}", mergedPath);
                Directory.Delete(mergedPath, true); // Recursive delete
            }
            Directory.CreateDirectory(mergedPath);
            logger.LogInformation("Initialized merged directory: {MergedPath}", mergedPath);
            return mergedPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize merged directory at {MergedPath}", mergedPath);
            return null;
        }
    }
}