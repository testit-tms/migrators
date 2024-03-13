using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;

namespace JsonWriter;

public class WriteService : IWriteService
{
    private readonly JsonSerializerOptions _options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };
    private readonly ILogger<WriteService> _logger;
    private readonly string _path;

    public WriteService(ILogger<WriteService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var path = configuration["resultPath"];
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Result path is not specified");
        }

        _path = Path.GetFullPath(path);
    }

    public async Task<string> WriteAttachment(Guid id, byte[] content, string fileName)
    {
        var fullPath = Path.Combine(_path, id.ToString());
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        var filePath = GetAttachmentPath(fileName, fullPath);

        _logger.LogInformation("Writing attachment {FileName} for test case {Id}: {Path}",
            fileName, id, filePath);

        await using var writer = new BinaryWriter(File.OpenWrite(filePath));
        writer.Write(content);

        return Path.GetFileName(filePath);
    }

    private static string GetAttachmentPath(string fileName, string directoryPath)
    {
        var files = Directory.GetFiles(directoryPath);

        if (files.Length == 0)
        {
            return Path.Combine(directoryPath, fileName);
        }

        var existFile =
            files.FirstOrDefault(f =>
                string.Equals(Path.GetFileName(f), fileName, StringComparison.InvariantCultureIgnoreCase));

        if (existFile == null)
        {
            return Path.Combine(directoryPath, fileName);
        }

        var newName = fileName;

        var i = 1;
        while (files.Any(f => Path.GetFileName(f) == newName))
        {
            newName = $"{Path.GetFileNameWithoutExtension(fileName)} ({i}){Path.GetExtension(fileName)}";
            i++;
        }

        return Path.Combine(directoryPath, newName);
    }

    public async Task WriteTestCase(TestCase testCase)
    {
        var fullPath = Path.Combine(_path, testCase.Id.ToString());
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        var filePath = Path.Combine(fullPath, Constants.TestCase);

        _logger.LogInformation("Writing test case {Id}: {Path}", testCase.Id, filePath);

        await using var createStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(createStream, testCase, _options);
    }

    public async Task WriteSharedStep(SharedStep sharedStep)
    {
        var fullPath = Path.Combine(_path, sharedStep.Id.ToString());
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        var filePath = Path.Combine(fullPath, Constants.SharedStep);

        _logger.LogInformation("Writing shared step {Id}: {Path}", sharedStep.Id, filePath);

        await using var createStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(createStream, sharedStep, _options);
    }

    public async Task WriteMainJson(Root mainJson)
    {
        var filePath = Path.Combine(_path, Constants.MainJson);

        _logger.LogInformation("Writing main.json: {Path}", filePath);

        await using var createStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(createStream, mainJson, _options);
    }
}
