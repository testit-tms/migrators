using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ZephyrSquadExporter.Client;

public class TokenManager
{
    private readonly ILogger<TokenManager> _logger;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _accountId;

    private const int ExpireTime = 3600;

    public TokenManager(ILogger<TokenManager> logger, IConfiguration configuration)
    {
        _logger = logger;

        var section = configuration.GetSection("zephyr");
        var accessKey = section["accessKey"];
        if (string.IsNullOrEmpty(accessKey))
        {
            throw new ArgumentException("Access key is not specified");
        }

        var secretKey = section["secretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new ArgumentException("Secret key is not specified");
        }

        var accountId = section["accountId"];
        if (string.IsNullOrEmpty(accountId))
        {
            throw new ArgumentException("Account ID is not specified");
        }

        _accessKey = accessKey;
        _secretKey = secretKey;
        _accountId = accountId;
    }

    public string GetToken(string methodType, string url)
    {
        _logger.LogDebug("Generating token for {MethodType} {Url}", methodType, url);

        var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var issueTime = DateTime.Now;
        var iat = (long)issueTime.Subtract(utc0).TotalMilliseconds;
        var exp = (long)issueTime.AddMilliseconds(ExpireTime).Subtract(utc0).TotalMilliseconds;

        var urls = url.Split('?');
        var canonicalPath = $"{methodType}&" + urls[0] + "&" + urls[1];

        var payload = new Dictionary<string, object>
        {
            { "sub", _accountId },
            { "qsh", GetQsh(canonicalPath) },
            { "iss", _accessKey },
            { "iat", iat },
            { "exp", exp }
        };

        var stringList = new List<string>();
        var dictionary = new Dictionary<string, object>
        {
            {
                "typ",
                "JWT"
            },
            {
                "alg",
                "HS256"
            }
        };

        var bytes1 = Encoding.UTF8.GetBytes(JsonSerializer.Serialize((object)dictionary));
        var bytes2 = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        stringList.Add(Base64UrlEncode(bytes1));
        stringList.Add(Base64UrlEncode(bytes2));
        var bytes3 = Encoding.UTF8.GetBytes(string.Join(".", stringList.ToArray()));
        var input = GetHash(_secretKey, bytes3);
        stringList.Add(Base64UrlEncode(input));

        return string.Join(".", stringList.ToArray());
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).Split('=')[0]
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] GetHash(string key, byte[] value)
    {
        using var hash = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return hash.ComputeHash(value);
    }

    private static string GetQsh(string url)
    {
        var hash = new StringBuilder();
        var crypto = SHA256.HashData(Encoding.UTF8.GetBytes(url));

        foreach (var theByte in crypto)
        {
            hash.Append(theByte.ToString("x2"));
        }

        return hash.ToString();
    }
}
