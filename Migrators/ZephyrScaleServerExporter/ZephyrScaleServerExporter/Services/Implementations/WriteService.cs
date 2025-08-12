using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using ZephyrScaleServerExporter.Models;

namespace ZephyrScaleServerExporter.Services.Implementations;

public class WriteService : IWriteService
{
    private readonly JsonSerializerOptions _options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };
    private readonly ILogger<WriteService> _logger;
    private readonly AppConfig _config;
    private readonly string _path;
    // created to synchronize file duplicates 
    private readonly ConcurrentDictionary<string, bool> _asyncWriteLock = new();
    
    public readonly int BatchNumber = 0;

    // <fileName, filePath> values for Shared Steps attachment copying to target testCase's folder
    private readonly ConcurrentDictionary<string, string> _attachmentStorage = new();
    public WriteService(
        ILogger<WriteService> logger, 
        IOptions<AppConfig> config)
    {
        _config = config.Value;
        _logger = logger;

        if (_config.Zephyr.Partial && !string.IsNullOrEmpty(_config.Zephyr.PartialFolderName))
        {
            (_path, BatchNumber) = InitBatchPath();
        }
        else
        {
            _path = Path.GetFullPath(_config.ResultPath);
        }
    }

    public int GetBatchNumber()
    {
        return BatchNumber;
    }

    private (string, int) InitBatchPath()
    {
        var basePath = Path.Combine(_config.ResultPath, 
            _config.Zephyr.ProjectKey, _config.Zephyr.PartialFolderName);
            
        int batchNumber = 1;
        var path = basePath + $"_{batchNumber}";
        while (Directory.Exists(path))
        {
            batchNumber++;
            path = basePath + $"_{batchNumber}";
        }
       
        return (Path.GetFullPath(path), batchNumber);
    }
    
    /// <summary>
    /// Using cached fileName:filePath dictionary, copying bytes for given fileName 
    /// to targetId's testCase destination. 
    /// Returns null if there is no copy proceed, or something wrong. 
    /// Allow second call for the same value (skip copy in this case, return fileName).
    /// </summary>
    public async Task<string?> CopyAttachment(Guid targetId, string fileName)
    {
        try
        {
            var sourcePath = GetSharedPath(fileName);
            if (sourcePath == "")
            {
                return null;
            }
            var content = await File.ReadAllBytesAsync(sourcePath);
            return await WriteAttachment(targetId, content, fileName, false);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    public bool IsAttachmentExist(Guid id, string fileName)
    {
        var fullPath = Path.Combine(_path, id.ToString());
        if (!Directory.Exists(fullPath))
        {
            return false;
        }
        var filePath = Path.Combine(fullPath, fileName);
        if (File.Exists(filePath)) 
        {
            _logger.LogInformation("Attachment {FileName} already exists for test case {Id}"
                ,fileName, id);
            return true;
        }
        return false;
    }

    private string GetSharedPath(string fileName)
    {
        return _attachmentStorage.GetValueOrDefault(fileName, "");
    }

    private bool IsLocked(string fullPath)
    {
        var isValue = _asyncWriteLock.TryGetValue(fullPath, out var value);
        return isValue && value;
    }

    public async Task<string> WriteAttachment(Guid testCaseId, byte[] content, string fileName, bool isSharedAttachment)
    {
        var fullPath = Path.Combine(_path, testCaseId.ToString());
        while (IsLocked(fullPath))
        {
            await Task.Delay(5);
        }
        _asyncWriteLock[fullPath] = true;
        try
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            var filePath = Path.Combine(fullPath, fileName);
            if (File.Exists(filePath))
            {
                _logger.LogInformation("Attachment {FileName} already exists for test case {Id}, skipping..."
                    , fileName, testCaseId);
                return Path.GetFileName(filePath);
            }

            _logger.LogInformation("Writing attachment {FileName}, bytes: {Bytes} for test case {Id}: {Path}",
                fileName, content.Length, testCaseId, filePath);

            await using var writer = new BinaryWriter(File.OpenWrite(filePath));
            writer.Write(content);
            if (isSharedAttachment)
            {
                _attachmentStorage.TryAdd(fileName, filePath);
            }
            return Path.GetFileName(filePath);
        }
        finally
        {
            _asyncWriteLock[fullPath] = false;
        }
    }

    public async Task WriteTestCase(global::Models.TestCase testCase)
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
        if (!Directory.Exists(_path))
        {
            Directory.CreateDirectory(_path);
        }
        await using var createStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(createStream, mainJson, _options);
    }
}
