using System.Text.Json;
using System.Text.Json.Serialization;
using Importer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;

namespace Importer.Services;

public class ParserService : IParserService
{
    private readonly ILogger<ParserService> _logger;
    private readonly string _resultPath;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private const string MainJsonFileName = "main.json";
    private const string SharedStepJsonFileName = "sharedstep.json";
    private const string TestCaseJsonFileName = "testcase.json";

    public ParserService(ILogger<ParserService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var resultPath = configuration["resultPath"];
        if (string.IsNullOrEmpty(resultPath))
        {
            throw new ArgumentException("resultPath is not set");
        }

        _resultPath = resultPath;
    }

    public async Task<Root> GetMainFile()
    {
        var mainJsonPath = Path.Combine(_resultPath, MainJsonFileName);
        if (!File.Exists(mainJsonPath))
        {
            _logger.LogError("Main json file not found: {Path}", mainJsonPath);
            throw new ApplicationException("Main json file not found");
        }

        var mainJson = await File.ReadAllTextAsync(mainJsonPath);
        var root = JsonSerializer.Deserialize<Root>(mainJson, _jsonSerializerOptions);

        if (root != null) return root;

        _logger.LogError("Main json file is empty: {Path}", mainJsonPath);
        throw new ApplicationException("Main json file is empty");
    }

    public async Task<SharedStep> GetSharedStep(Guid guid)
    {
        var sharedStepPath = Path.Combine(_resultPath, guid.ToString(), SharedStepJsonFileName);
        if (!File.Exists(sharedStepPath))
        {
            _logger.LogError("Shared step file not found: {Path}", sharedStepPath);
            throw new ApplicationException("Shared step file not found");
        }

        var sharedStepJson = await File.ReadAllTextAsync(sharedStepPath);
        var step = JsonSerializer.Deserialize<SharedStep>(sharedStepJson, _jsonSerializerOptions);

        if (step != null) return step;

        _logger.LogError("Shared step file is empty: {Path}", sharedStepPath);
        throw new ApplicationException("Shared step file is empty");
    }

    public async Task<TestCase> GetTestCase(Guid guid)
    {
        var testCasePath = Path.Combine(_resultPath, guid.ToString(), TestCaseJsonFileName);
        if (!File.Exists(testCasePath))
        {
            _logger.LogError("Test case file not found: {Path}", testCasePath);
            throw new ApplicationException("Test case file not found");
        }

        var testCaseJson = await File.ReadAllTextAsync(testCasePath);
        var testCase = JsonSerializer.Deserialize<TestCase>(testCaseJson, _jsonSerializerOptions);

        if (testCase != null) return testCase;

        _logger.LogError("Test case file is empty: {Path}", testCasePath);
        throw new ApplicationException("Test case file is empty");
    }

    public async Task<Stream> GetAttachment(Guid guid, string fileName)
    {
        var filePath = Path.Combine(_resultPath, guid.ToString(), fileName);
        if (!File.Exists(filePath))
        {
            _logger.LogError("Attachment file not found: {Path}", filePath);
            throw new ApplicationException("Attachment file not found");
        }

        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }
}
