using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.TestCases;

namespace ZephyrScaleServerExporter.Services.Implementations;

/// <summary>
/// Service for logging errors encountered during test case processing to a dedicated file.
/// </summary>
public class TestCaseErrorLogService : ITestCaseErrorLogService
{
    private readonly ILogger<TestCaseErrorLogService> _logger;
    private readonly AppConfig _config;
    private readonly string _errorLogDirectory;

    public TestCaseErrorLogService(ILogger<TestCaseErrorLogService> logger, IOptions<AppConfig> config)
    {
        _logger = logger;
        _config = config.Value;
        // Define a specific subdirectory for these error logs within the main result path
        _errorLogDirectory = Path.Combine(_config.ResultPath, "error_logs");
        try
        {
            Directory.CreateDirectory(_errorLogDirectory); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory for test case error logs at {ErrorLogDirectory}", _errorLogDirectory);
            // If directory creation fails, subsequent logging attempts will also likely fail for file operations.
            // Depending on requirements, could throw here or try a fallback path.
        }
    }

    /// <summary>
    /// Logs an error related to test case processing to a timestamped file.
    /// </summary>
    public void LogError(Exception ex, string contextMessage, ZephyrTestCase? problematicTestCase = null, IEnumerable<ZephyrTestCase>? associatedTestCases = null)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string fileName = $"testcase_processing_error_{timestamp}.log";
            string filePath = Path.Combine(_errorLogDirectory, fileName);

            var sb = new StringBuilder();
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"Context: {contextMessage}");
            sb.AppendLine("--- Exception Details ---");
            sb.AppendLine(ex.ToString());
            sb.AppendLine("-------------------------");

            if (problematicTestCase != null)
            {
                sb.AppendLine("Problematic Test Case:");
                sb.AppendLine($"  Key: {problematicTestCase.Key ?? "N/A"}, Name: {problematicTestCase.Name ?? "N/A"}");
                sb.AppendLine("-------------------------");
            }

            if (associatedTestCases != null)
            {
                var zephyrTestCases = associatedTestCases.ToList();
                if (zephyrTestCases.Count != 0)
                {
                    sb.AppendLine("Associated Test Cases in Current Batch/Context:");
                    foreach (var tc in zephyrTestCases)
                    {
                        sb.AppendLine($"  Key: {tc.Key ?? "N/A"}, Name: {tc.Name ?? "N/A"}");
                    }
                    sb.AppendLine("-------------------------");
                }
            }

            File.AppendAllText(filePath, sb.ToString());
            _logger.LogInformation("Test case processing error logged to: {FilePath}", filePath);
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "Failed to write to the test case error log file.");
            // Fallback logging to the main logger if file logging fails
            _logger.LogError("Original Error Context: {Context}. Original Exception: {Exception}", contextMessage, ex.ToString());
        }
    }
} 