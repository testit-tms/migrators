using System.Net;
using System.Text.Json;
using AllureExporter.Client;
using AllureExporter.Models.Config;
using AllureExporter.Models.Project;
using AllureExporter.Models.TestCase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace AllureExporterTests;

public sealed class ClientTests : IDisposable
{
    private const string BaseUrl = "http://test.allure.com";
    private const string ProjectName = "TestProject";
    private const string ApiToken = "test-token";
    private const string BearerToken = "bearer-token";
    private const long DefaultProjectId = 1;
    private const long DefaultTestCaseId = 1;

    private Mock<ILogger<Client>> _logger = null!;
    private Mock<IOptions<AppConfig>> _config = null!;
    private Mock<HttpMessageHandler> _httpHandler = null!;
    private HttpClient _httpClient = null!;
    private Client _sut = null!;
    private bool _disposed;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<Client>>();
        _config = new Mock<IOptions<AppConfig>>();
        _httpHandler = new Mock<HttpMessageHandler>();

        _config.Setup(x => x.Value).Returns(new AppConfig
        {
            Allure = new AllureConfig
            {
                Url = BaseUrl,
                ProjectName = ProjectName,
                ApiToken = ApiToken,
                MigrateAutotests = true
            }
        });

        _httpClient = new HttpClient(_httpHandler.Object);
        _sut = new TestClient(_logger.Object, _config.Object, _httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        Dispose(true);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _httpClient?.Dispose();
            (_sut as IDisposable)?.Dispose();
        }

        _disposed = true;
    }

    private sealed class TestClient(ILogger<Client> logger, IOptions<AppConfig> config, HttpClient httpClient)
        : Client(logger, config, httpClient) { }

    [Test]
    public async Task GetProjectId_WhenProjectExists_ReturnsProject()
    {
        // Arrange
        var projects = new BaseEntities
        {
            Content = new List<BaseEntity>
            {
                new() { Id = DefaultProjectId, Name = ProjectName },
                new() { Id = 2, Name = "Other Project" }
            }
        };

        SetupHttpHandler("api/rs/project", HttpStatusCode.OK, projects);

        // Act
        var result = await _sut.GetProjectId();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(DefaultProjectId));
            Assert.That(result.Name, Is.EqualTo(ProjectName));
        });

        VerifyHttpCall("api/rs/project");
    }

    [Test]
    public async Task GetProjectId_WhenProjectNotFound_ThrowsException()
    {
        // Arrange
        var projects = new BaseEntities
        {
            Content = new List<BaseEntity>
            {
                new() { Id = 1, Name = "Other Project" }
            }
        };

        SetupHttpHandler<BaseEntities>("api/rs/project", HttpStatusCode.OK, projects);

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(async () => await _sut.GetProjectId());
        Assert.That(ex!.Message, Is.EqualTo("Project not found"));

        VerifyHttpCall("api/rs/project");
    }

    [Test]
    public async Task GetTestCaseIdsFromMainSuite_WhenTestCasesExist_ReturnsIds()
    {
        // Arrange
        var testCases = new AllureTestCases
        {
            Content = new List<AllureTestCaseBase>
            {
                new() { Id = 1, Automated = false },
                new() { Id = 2, Automated = true },
                new() { Id = 3, Automated = false }
            },
            TotalPages = 1
        };

        SetupHttpHandler<AllureTestCases>(
            $"api/rs/testcasetree/leaf?projectId={DefaultProjectId}&treeId=2&page=0",
            HttpStatusCode.OK,
            testCases);

        // Act
        var result = await _sut.GetTestCaseIdsFromMainSuite(DefaultProjectId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result, Does.Contain(1));
            Assert.That(result, Does.Contain(2));
            Assert.That(result, Does.Contain(3));
        });

        VerifyHttpCall($"api/rs/testcasetree/leaf?projectId={DefaultProjectId}&treeId=2&page=0");
    }

    [Test]
    public async Task GetTestCaseById_WhenTestCaseExists_ReturnsTestCase()
    {
        // Arrange
        var testCase = new AllureTestCase
        {
            Id = DefaultTestCaseId,
            Name = "Test Case",
            Description = "Description",
            Status = new Status { Name = "Active" }
        };

        SetupHttpHandler<AllureTestCase>(
            $"api/rs/testcase/{DefaultTestCaseId}",
            HttpStatusCode.OK,
            testCase);

        // Act
        var result = await _sut.GetTestCaseById(DefaultTestCaseId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(DefaultTestCaseId));
            Assert.That(result.Name, Is.EqualTo("Test Case"));
            Assert.That(result.Description, Is.EqualTo("Description"));
            Assert.That(result.Status.Name, Is.EqualTo("Active"));
        });

        VerifyHttpCall($"api/rs/testcase/{DefaultTestCaseId}");
    }

    [Test]
    public async Task GetCustomFieldsFromTestCase_WhenFieldsExist_ReturnsFields()
    {
        // Arrange
        var customFields = new List<AllureCustomField>
        {
            new()
            {
                CustomField = new CustomField { Name = "Feature" },
                Name = "Feature1"
            },
            new()
            {
                CustomField = new CustomField { Name = "Story" },
                Name = "Story1"
            }
        };

        SetupHttpHandler<List<AllureCustomField>>(
            $"api/rs/testcase/{DefaultTestCaseId}/cfv",
            HttpStatusCode.OK,
            customFields);

        // Act
        var result = await _sut.GetCustomFieldsFromTestCase(DefaultTestCaseId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].CustomField.Name, Is.EqualTo("Feature"));
            Assert.That(result[0].Name, Is.EqualTo("Feature1"));
            Assert.That(result[1].CustomField.Name, Is.EqualTo("Story"));
            Assert.That(result[1].Name, Is.EqualTo("Story1"));
        });

        VerifyHttpCall($"api/rs/testcase/{DefaultTestCaseId}/cfv");
    }

    [Test]
    public async Task GetCustomFieldsFromTestCase_WhenApiError_ThrowsException()
    {
        // Arrange
        SetupHttpHandler<List<AllureCustomField>>(
            $"api/rs/testcase/{DefaultTestCaseId}/cfv",
            HttpStatusCode.NotFound,
            null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(
            async () => await _sut.GetCustomFieldsFromTestCase(DefaultTestCaseId));
        Assert.That(ex!.Message, Does.Contain("Failed to get custom fields"));

        VerifyHttpCall($"api/rs/testcase/{DefaultTestCaseId}/cfv");
    }

    private void SetupHttpHandler<T>(string requestUri, HttpStatusCode statusCode, T? content)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            response.Content = new StringContent(JsonSerializer.Serialize(content));
        }

        _httpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyHttpCall(string requestUri)
    {
        _httpHandler
            .Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>());
    }
}
