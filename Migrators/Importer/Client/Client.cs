using Importer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using TestIt.Client.Api;
using TestIt.Client.Client;
using TestIt.Client.Model;
using Attribute = Models.Attribute;
using LinkType = TestIt.Client.Model.LinkType;

namespace Importer.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly AttachmentsApi _attachments;
    private readonly ProjectsApi _projectsApi;
    private readonly SectionsApi _sectionsApi;
    private readonly CustomAttributesApi _customAttributes;
    private readonly WorkItemsApi _workItemsApi;
    private readonly CustomAttributesApi _customAttributesApi;
    private readonly ParametersApi _parametersApi;
    private Guid _projectId;

    private const int TenMinutes = 60000;

    public Client(ILogger<Client> logger, IConfiguration configuration)
    {
        _logger = logger;

        var tmsSection = configuration.GetSection("tms");
        var url = tmsSection["url"];
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("TMS url is not specified");
        }

        var token = tmsSection["privateToken"];
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("TMS private token is not specified");
        }

        var certValidation = true;
        var certValidationStr = tmsSection["certValidation"];
        if (!string.IsNullOrEmpty(certValidationStr))
        {
            certValidation = bool.Parse(certValidationStr);
        }

        var cfg = new Configuration { BasePath = url.TrimEnd('/') };
        cfg.AddApiKeyPrefix("Authorization", "PrivateToken");
        cfg.AddApiKey("Authorization", token);

        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => certValidation;

        _attachments = new AttachmentsApi(new HttpClient(), cfg, httpClientHandler);
        _projectsApi = new ProjectsApi(new HttpClient(), cfg, httpClientHandler);
        _sectionsApi = new SectionsApi(new HttpClient(), cfg, httpClientHandler);
        _customAttributes = new CustomAttributesApi(new HttpClient(), cfg, httpClientHandler);
        _workItemsApi = new WorkItemsApi(new HttpClient(), cfg, httpClientHandler);
        _customAttributesApi = new CustomAttributesApi(new HttpClient(), cfg, httpClientHandler);
        _parametersApi = new ParametersApi(new HttpClient(), cfg, httpClientHandler);
    }

    public async Task CreateProject(string name)
    {
        _logger.LogInformation("Creating project {Name}", name);

        try
        {
            var resp = await _projectsApi.CreateProjectAsync(new ProjectPostModel(name: name));
            _projectId = resp.Id;

            _logger.LogDebug("Created project {@Project}", resp);
            _logger.LogInformation("Created project {Name} with id {Id}", name, resp.Id);
        }
        catch (Exception e)
        {
            _logger.LogError("Could not create project {Name}: {Message}", name, e.Message);
            throw;
        }
    }

    public async Task<Guid> ImportSection(Guid parentSectionId, Section section)
    {
        _logger.LogInformation("Importing section {Name}", section.Name);

        try
        {
            var model = new SectionPostModel(name: section.Name, parentId: parentSectionId, projectId: _projectId)
            {
                PostconditionSteps = section.PostconditionSteps.Select(s => new StepPutModel
                {
                    Action = s.Action,
                    Expected = s.Expected
                }).ToList(),
                PreconditionSteps = section.PreconditionSteps.Select(s => new StepPutModel
                {
                    Action = s.Action,
                    Expected = s.Expected
                }).ToList()
            };

            _logger.LogDebug("Importing section {@Section}", model);

            var resp = await _sectionsApi.CreateSectionAsync(model);

            _logger.LogDebug("Imported section {@Section}", resp);
            _logger.LogInformation("Imported section {Name} with id {Id}", section.Name, resp.Id);

            return resp.Id;
        }
        catch (Exception e)
        {
            _logger.LogError("Could not import section {Name}: {Message}", section.Name, e.Message);
            throw;
        }
    }

    public async Task<TmsAttribute> ImportAttribute(Attribute attribute)
    {
        _logger.LogInformation("Importing attribute {Name}", attribute.Name);

        try
        {
            var model = new GlobalCustomAttributePostModel(name: attribute.Name)
            {
                Type = Enum.Parse<CustomAttributeTypesEnum>(attribute.Type.ToString()),
                IsRequired = attribute.IsRequired,
                IsEnabled = attribute.IsActive,
                Options = attribute.Options.Select(o => new CustomAttributeOptionPostModel(value: o)).ToList()
            };

            _logger.LogDebug("Importing attribute {@Attribute}", model);

            var resp = await _customAttributes.ApiV2CustomAttributesGlobalPostAsync(model);

            _logger.LogDebug("Imported attribute {@Attribute}", resp);
            _logger.LogInformation("Imported attribute {Name} with id {Id}", attribute.Name, resp.Id);

            return new TmsAttribute
            {
                Id = resp.Id,
                Name = resp.Name,
                Type = resp.Type.ToString(),
                IsRequired = resp.IsRequired,
                IsEnabled = resp.IsEnabled,
                Options = resp.Options.Select(o => new TmsAttributeOptions()
                {
                    Id = o.Id,
                    Value = o.Value,
                    IsDefault = o.IsDefault
                }).ToList()
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Could not import attribute {Name}: {Message}", attribute.Name, e.Message);
            throw;
        }
    }

    public async Task<Guid> ImportSharedStep(Guid parentSectionId, SharedStep sharedStep)
    {
        try
        {
            var model = new WorkItemPostModel(
                steps: new List<StepPutModel>(),
                preconditionSteps: new List<StepPutModel>(),
                postconditionSteps: new List<StepPutModel>(),
                attributes: new Dictionary<string, object>(),
                links: new List<LinkPostModel>(),
                tags: new List<TagShortModel>(),
                name: sharedStep.Name)
            {
                EntityTypeName = WorkItemEntityTypes.SharedSteps,
                Description = sharedStep.Description,
                SectionId = parentSectionId,
                State = Enum.Parse<WorkItemStates>(sharedStep.State.ToString()),
                Priority = Enum.Parse<WorkItemPriorityModel>(sharedStep.Priority.ToString()),
                Steps = sharedStep.Steps.Select(s =>
                    new StepPutModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                Attributes = sharedStep.Attributes
                    .ToDictionary(keySelector: a => a.Id.ToString(),
                        elementSelector: a => (object)a.Value),
                Tags = sharedStep.Tags.Select(t => new TagShortModel(t)).ToList(),
                Links = sharedStep.Links.Select(l =>
                    new LinkPostModel(url: l.Url)
                    {
                        Title = l.Title,
                        Description = l.Description,
                        Type = Enum.Parse<LinkType>(l.Type.ToString())
                    }).ToList(),
                Name = sharedStep.Name,
                ProjectId = _projectId,
                Attachments = sharedStep.Attachments.Select(a => new AttachmentPutModel(Guid.Parse(a))).ToList()
            };

            _logger.LogDebug("Importing shared step {Name} and {@Model}", sharedStep.Name, model);

            var resp = await _workItemsApi.CreateWorkItemAsync(model);

            _logger.LogDebug("Imported shared step {@SharedStep}", resp);

            _logger.LogInformation("Imported shared step {Name} with id {Id}", sharedStep.Name, resp.Id);

            return resp.Id;
        }
        catch (Exception e)
        {
            _logger.LogError("Could not import shared step {Name}: {Message}", sharedStep.Name, e.Message);
            throw;
        }
    }

    public async Task ImportTestCase(Guid parentSectionId, TmsTestCase testCase)
    {
        _logger.LogInformation("Importing test case {Name}", testCase.Name);

        try
        {
            var model = new WorkItemPostModel(
                steps: new List<StepPutModel>(),
                preconditionSteps: new List<StepPutModel>(),
                postconditionSteps: new List<StepPutModel>(),
                attributes: new Dictionary<string, object>(),
                links: new List<LinkPostModel>(),
                tags: new List<TagShortModel>(),
                name: testCase.Name)
            {
                EntityTypeName = WorkItemEntityTypes.TestCases,
                SectionId = parentSectionId,
                State = Enum.Parse<WorkItemStates>(testCase.State.ToString()),
                Priority = Enum.Parse<WorkItemPriorityModel>(testCase.Priority.ToString()),
                PreconditionSteps = testCase.PreconditionSteps.Select(s =>
                    new StepPutModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                PostconditionSteps = testCase.PostconditionSteps.Select(s =>
                    new StepPutModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                Steps = testCase.Steps.Select(s =>
                    new StepPutModel
                    {
                        Action = s.Action,
                        Expected = s.Expected,
                        WorkItemId = s.SharedStepId
                    }).ToList(),
                Attributes = testCase.Attributes
                    .ToDictionary(keySelector: a => a.Id.ToString(),
                        elementSelector: a => (object)a.Value),
                Tags = testCase.Tags.Select(t => new TagShortModel(t)).ToList(),
                Links = testCase.Links.Select(l =>
                    new LinkPostModel(url: l.Url)
                    {
                        Title = l.Title,
                        Description = l.Description,
                        Type = Enum.Parse<LinkType>(l.Type.ToString())
                    }).ToList(),
                Name = testCase.Name,
                ProjectId = _projectId,
                Attachments = testCase.Attachments.Select(a => new AttachmentPutModel(Guid.Parse(a))).ToList(),
                Iterations = testCase.TmsIterations.Select(i =>
                {
                    var parameters = i.Parameters.Select(p => new ParameterIterationModel(p)).ToList();
                    return new IterationPutModel(parameters: parameters);
                }).ToList(),
                Duration = testCase.Duration == 0 ? TenMinutes : testCase.Duration,
                Description = testCase.Description
            };

            _logger.LogDebug("Importing test case {Name} and {@Model}", testCase.Name, model);

            var resp = await _workItemsApi.CreateWorkItemAsync(model);

            _logger.LogDebug("Imported test case {@TestCase}", resp);

            _logger.LogInformation("Imported test case {Name} with id {Id}", testCase.Name, resp.Id);
        }
        catch (Exception e)
        {
            _logger.LogError("Could not import test case {Name}: {Message}", testCase.Name, e.Message);
            throw;
        }
    }

    public async Task<Guid> GetRootSectionId()
    {
        _logger.LogInformation("Getting root section id");

        try
        {
            var section = await _projectsApi.GetSectionsByProjectIdAsync(_projectId.ToString());

            _logger.LogDebug("Got root section {@Section}", section.First());

            return section.First().Id;
        }
        catch (Exception e)
        {
            _logger.LogError("Could not get root section id: {Message}", e.Message);
            throw;
        }
    }

    public async Task<List<TmsAttribute>> GetProjectAttributes()
    {
        _logger.LogInformation("Getting project attributes");

        try
        {
            var attributes = await _customAttributesApi.ApiV2CustomAttributesSearchPostAsync(
                customAttributeSearchQueryModel: new CustomAttributeSearchQueryModel(isGlobal: true, isDeleted: false));

            _logger.LogDebug("Got project attributes {@Attributes}", attributes);

            return attributes.Select(a => new TmsAttribute
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type.ToString(),
                IsEnabled = a.IsEnabled,
                IsRequired = a.IsRequired,
                IsGlobal = a.IsGlobal,
                Options = a.Options.Select(o => new TmsAttributeOptions
                {
                    Id = o.Id,
                    Value = o.Value,
                    IsDefault = o.IsDefault
                }).ToList()
            }).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError("Could not get project attributes: {Message}", e.Message);
            throw;
        }
    }

    public async Task AddAttributesToProject(IEnumerable<Guid> attributeIds)
    {
        _logger.LogInformation("Adding attributes to project");

        try
        {
            await _projectsApi.AddGlobaAttributesToProjectAsync(_projectId.ToString(), attributeIds.ToList());
        }
        catch (Exception e)
        {
            _logger.LogError("Could not add attributes to project: {Message}", e.Message);
            throw;
        }
    }

    public async Task<TmsAttribute> UpdateAttribute(TmsAttribute attribute)
    {
        _logger.LogInformation("Updating attribute {Name}", attribute.Name);

        try
        {
            var model = new GlobalCustomAttributeUpdateModel(name: attribute.Name)
            {
                IsEnabled = attribute.IsEnabled,
                IsRequired = attribute.IsRequired,
                Options = attribute.Options.Select(o => new CustomAttributeOptionModel()
                {
                    Id = o.Id,
                    Value = o.Value,
                    IsDefault = o.IsDefault
                }).ToList()
            };

            _logger.LogDebug("Updating attribute {@Model}", model);

            var resp = await _customAttributesApi.ApiV2CustomAttributesGlobalIdPutAsync(id: attribute.Id,
                globalCustomAttributeUpdateModel: model);

            _logger.LogDebug("Updated attribute {@Response}", resp);

            attribute.Options = resp.Options.Select(o => new TmsAttributeOptions()
            {
                Id = o.Id,
                Value = o.Value,
                IsDefault = o.IsDefault
            }).ToList();

            return attribute;
        }

        catch (Exception e)
        {
            _logger.LogError("Could not update attribute {Name}: {Message}", attribute.Name, e.Message);
            throw;
        }
    }

    public async Task<Guid> UploadAttachment(string fileName, Stream content)
    {
        _logger.LogDebug("Uploading attachment {Name}", fileName);

        try
        {
            var response = await _attachments.ApiV2AttachmentsPostAsync(
                new FileParameter(
                    filename: Path.GetFileName(fileName),
                    content: content,
                    contentType: MimeTypes.GetMimeType(fileName)));

            _logger.LogDebug("Uploaded attachment {@Response}", response);

            return response.Id;
        }
        catch (Exception e)
        {
            _logger.LogError("Could not upload attachment {Name}: {Message}", fileName, e.Message);
            throw;
        }
    }

    public async Task<TmsParameter> CreateParameter(Parameter parameter)
    {
        _logger.LogInformation("Creating parameter {Name}", parameter.Name);

        try
        {
            var model = new ParameterPostModel(name: parameter.Name,
                value: parameter.Value);

            _logger.LogDebug("Creating parameter {@Model}", model);

            var resp = await _parametersApi.CreateParameterAsync(model);

            _logger.LogDebug("Created parameter {@Response}", resp);

            return new TmsParameter
            {
                Id = resp.Id,
                Value = resp.Value,
                Name = resp.Name,
                ParameterKeyId = resp.ParameterKeyId
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Could not create parameter {Name}: {Message}", parameter.Name, e.Message);
            throw;
        }
    }

    public async Task<List<TmsParameter>> GetParameter(string name)
    {
        _logger.LogInformation("Getting parameter {Name}", name);

        try
        {
            var resp = await _parametersApi.ApiV2ParametersSearchPostAsync(
                parameterFilterModel: new ParameterFilterModel(name: name, isDeleted: false));

            _logger.LogDebug("Got parameter {@Response}", resp);

            return resp.Select(p =>
                    new TmsParameter()
                    {
                        Id = p.Id,
                        Value = p.Value,
                        Name = p.Name,
                        ParameterKeyId = p.ParameterKeyId
                    })
                .ToList();
        }
        catch (Exception e)
        {
            _logger.LogError("Could not get parameter {Name}: {Message}", name, e.Message);
            throw;
        }
    }
}
