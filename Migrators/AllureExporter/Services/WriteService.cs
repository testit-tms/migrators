using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter.Services;

public class WriteService : IWriteService
{
    private readonly ILogger<WriteService> _logger;
    private readonly string _path;

    public WriteService(ILogger<WriteService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var path = configuration.GetValue<string>("resultPath");
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Result path is not specified");
        }

        _path = path;
    }

    public async Task WriteAttachment(Guid id, byte[] content, string fileName)
    {
        _logger.LogInformation("Writing attachment {FileName} for test case {Id}", fileName, id);

        var fullPath = Path.Combine(_path, id.ToString());
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        var filePath = Path.Combine(fullPath, fileName);
        await using var writer = new BinaryWriter(File.OpenWrite(filePath));
        writer.Write(content);
    }

    public async Task WriteTestCase(TestCase testCase)
    {
        _logger.LogInformation("Writing test case {Id}", testCase.Id);

        var fullPath = Path.Combine(_path, testCase.Id.ToString());
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        var filePath = Path.Combine(fullPath, "testcase.json");
        var content = JsonSerializer.Serialize(testCase);

        await using var writer = new BinaryWriter(File.OpenWrite(filePath));
        writer.Write(content);
    }

    public async Task WriteMainJson(Root mainJson)
    {
        var filePath = Path.Combine(_path, "main.json");
        var content = JsonSerializer.Serialize(mainJson);

        await using var writer = new BinaryWriter(File.OpenWrite(filePath));
        writer.Write(content);
    }
}
