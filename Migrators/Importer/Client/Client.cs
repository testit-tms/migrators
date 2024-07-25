using Importer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using System;
using System.Xml.Linq;
using TestIT.ApiClient.Api;
using TestIT.ApiClient.Client;
using TestIT.ApiClient.Model;
using Attribute = Models.Attribute;
using LinkType = TestIT.ApiClient.Model.LinkType;

namespace Importer.Client;

public class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly AttachmentsApi _attachments;
    private readonly ProjectsApi _projectsApi;
    private readonly ProjectAttributesApi _projectAttributesApi;
    private readonly ProjectSectionsApi _projectSectionsApi;
    private readonly SectionsApi _sectionsApi;
    private readonly CustomAttributesApi _customAttributes;
    private readonly WorkItemsApi _workItemsApi;
    private readonly CustomAttributesApi _customAttributesApi;
    private readonly ParametersApi _parametersApi;
    private readonly bool _importToExistingProject;
    private readonly string _projectName;

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

        _projectName = tmsSection["projectName"];
        if (!string.IsNullOrEmpty(_projectName))
        {
            _logger.LogInformation("Import by custom project name {Name}", _projectName);
        }

        _importToExistingProject = false;
        var importToExistingProjectStr = tmsSection["importToExistingProject"];
        if (!string.IsNullOrEmpty(importToExistingProjectStr))
        {
            _importToExistingProject = bool.Parse(importToExistingProjectStr);
        }

        var cfg = new Configuration { BasePath = url.TrimEnd('/') };
        cfg.AddApiKeyPrefix("Authorization", "PrivateToken");
        cfg.AddApiKey("Authorization", token);

        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => certValidation;

        _attachments = new AttachmentsApi(new HttpClient(), cfg, httpClientHandler);
        _projectsApi = new ProjectsApi(new HttpClient(), cfg, httpClientHandler);
        _projectAttributesApi = new ProjectAttributesApi(new HttpClient(), cfg, httpClientHandler);
        _projectSectionsApi = new ProjectSectionsApi(new HttpClient(), cfg, httpClientHandler);
        _sectionsApi = new SectionsApi(new HttpClient(), cfg, httpClientHandler);
        _customAttributes = new CustomAttributesApi(new HttpClient(), cfg, httpClientHandler);
        _workItemsApi = new WorkItemsApi(new HttpClient(), cfg, httpClientHandler);
        _customAttributesApi = new CustomAttributesApi(new HttpClient(), cfg, httpClientHandler);
        _parametersApi = new ParametersApi(new HttpClient(), cfg, httpClientHandler);
    }

    public async Task<Guid> GetProject(string name)
    {
        if (!string.IsNullOrEmpty(_projectName))
        {
            name = _projectName;
        }

        _logger.LogInformation("Getting project {Name}", name);

        try
        {
            var projects = await _projectsApi.ApiV2ProjectsSearchPostAsync(null, null, null, null, null, new ApiV2ProjectsSearchPostRequest(name: name));

            _logger.LogDebug("Got projects {@Project} by name {Name}", projects, name);

            if (projects.Count != 0)
            {
                foreach (var project in projects)
                {
                    if (project.Name == name)
                    {
                        _logger.LogInformation("Got project {Name} with id {Id}", project.Name, project.Id);

                        if (!_importToExistingProject)
                        {
                            throw new Exception("Project with the same name already exists");
                        }

                        return project.Id;
                    }
                }
            }

            return Guid.Empty;
        }
        catch (Exception e)
        {
            _logger.LogError("Project {Name}: {Message}", name, e.Message);
            throw;
        }
    }

    public async Task<Guid> CreateProject(string name)
    {
        if (!string.IsNullOrEmpty(_projectName))
        {
            name = _projectName;
        }

        _logger.LogInformation("Creating project {Name}", name);

        try
        {
            var resp = await _projectsApi.CreateProjectAsync(new CreateProjectRequest(name: name));

            _logger.LogDebug("Created project {@Project}", resp);
            _logger.LogInformation("Created project {Name} with id {Id}", name, resp.Id);

            return resp.Id;
        }
        catch (Exception e)
        {
            _logger.LogError("Could not create project {Name}: {Message}", name, e.Message);
            throw;
        }
    }

    public async Task<Guid> GetSection(Guid projectId, Guid parentSectionId, Section section)
    {
        _logger.LogInformation("Importing section {Name}", section.Name);

        try
        {
            var model = new CreateSectionRequest(name: section.Name, parentId: parentSectionId, projectId: projectId, attachments: [])
            {
                PostconditionSteps = section.PostconditionSteps.Select(s => new StepPostModel
                {
                    Action = s.Action,
                    Expected = s.Expected
                }).ToList(),
                PreconditionSteps = section.PreconditionSteps.Select(s => new StepPostModel
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

    public async Task<Guid> ImportSection(Guid projectId, Guid parentSectionId, Section section)
    {
        _logger.LogInformation("Importing section {Name}", section.Name);

        try
        {
            var model = new CreateSectionRequest(name: section.Name, parentId: parentSectionId, projectId: projectId, attachments: [])
            {
                PostconditionSteps = section.PostconditionSteps.Select(s => new StepPostModel
                {
                    Action = s.Action,
                    Expected = s.Expected
                }).ToList(),
                PreconditionSteps = section.PreconditionSteps.Select(s => new StepPostModel
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
            var model = new ApiV2CustomAttributesGlobalPostRequest(name: attribute.Name)
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

    public async Task<TmsAttribute> GetAttribute(Guid id)
    {
        _logger.LogInformation("Getting attribute {Id}", id);

        try
        {
            var resp = await _customAttributesApi.ApiV2CustomAttributesIdGetAsync(id: id);

            _logger.LogDebug("Got attribute {@Attribute}", resp);

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
            _logger.LogError("Could not get attribute {Id}: {Message}", id, e.Message);
            throw;
        }
    }

    public async Task<Guid> ImportSharedStep(Guid projectId, Guid parentSectionId, SharedStep sharedStep)
    {
        try
        {
            var model = new CreateWorkItemRequest(
                steps: new List<StepPostModel>(),
                preconditionSteps: new List<StepPostModel>(),
                postconditionSteps: new List<StepPostModel>(),
                attributes: new Dictionary<string, object>(),
                links: new List<LinkPostModel>(),
                tags: new List<TagPostModel>(),
                name: sharedStep.Name)
            {
                EntityTypeName = WorkItemEntityTypes.SharedSteps,
                Description = sharedStep.Description,
                SectionId = parentSectionId,
                State = Enum.Parse<WorkItemStates>(sharedStep.State.ToString()),
                Priority = Enum.Parse<WorkItemPriorityModel>(sharedStep.Priority.ToString()),
                Steps = sharedStep.Steps.Select(s =>
                    new StepPostModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                Attributes = sharedStep.Attributes
                    .ToDictionary(keySelector: a => a.Id.ToString(),
                        elementSelector: a => (object)a.Value),
                Tags = sharedStep.Tags.Select(t => new TagPostModel(t)).ToList(),
                Links = sharedStep.Links.Select(l =>
                    new LinkPostModel(url: l.Url)
                    {
                        Title = l.Title,
                        Description = l.Description,
                        Type = Enum.Parse<LinkType>(l.Type.ToString())
                    }).ToList(),
                Name = sharedStep.Name,
                ProjectId = projectId,
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

    public async Task ImportTestCase(Guid projectId, Guid parentSectionId, TmsTestCase testCase)
    {
        _logger.LogInformation("Importing test case {Name}", testCase.Name);

        try
        {
            var model = new CreateWorkItemRequest(
                steps: new List<StepPostModel>(),
                preconditionSteps: new List<StepPostModel>(),
                postconditionSteps: new List<StepPostModel>(),
                attributes: new Dictionary<string, object>(),
                links: new List<LinkPostModel>(),
                tags: new List<TagPostModel>(),
                name: testCase.Name)
            {
                EntityTypeName = WorkItemEntityTypes.TestCases,
                SectionId = parentSectionId,
                State = Enum.Parse<WorkItemStates>(testCase.State.ToString()),
                Priority = Enum.Parse<WorkItemPriorityModel>(testCase.Priority.ToString()),
                PreconditionSteps = testCase.PreconditionSteps.Select(s =>
                    new StepPostModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                PostconditionSteps = testCase.PostconditionSteps.Select(s =>
                    new StepPostModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                Steps = testCase.Steps.Select(s =>
                    new StepPostModel
                    {
                        Action = s.Action,
                        Expected = s.Expected,
                        WorkItemId = s.SharedStepId,
                        TestData = s.TestData
                    }).ToList(),
                Attributes = testCase.Attributes
                    .ToDictionary(keySelector: a => a.Id.ToString(),
                        elementSelector: a => (object)a.Value),
                Tags = testCase.Tags.Select(t => new TagPostModel(t)).ToList(),
                Links = testCase.Links.Select(l =>
                    new LinkPostModel(url: l.Url)
                    {
                        Title = l.Title,
                        Description = l.Description,
                        Type = Enum.Parse<LinkType>(l.Type.ToString())
                    }).ToList(),
                Name = testCase.Name,
                ProjectId = projectId,
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

    public async Task<Guid> GetRootSectionId(Guid projectId)
    {
        _logger.LogInformation("Getting root section id");

        try
        {
            var section = await _projectSectionsApi.GetSectionsByProjectIdAsync(projectId.ToString());

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
                apiV2CustomAttributesSearchPostRequest: new ApiV2CustomAttributesSearchPostRequest(isGlobal: true, isDeleted: false));

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

    public async Task<List<TmsAttribute>> GetRequiredProjectAttributesByProjectId(Guid projectId)
    {
        _logger.LogInformation("Getting required project attributes by project id {Id}", projectId);

        try
        {
            var attributes = await _projectAttributesApi.SearchAttributesInProjectAsync(
                projectId: projectId.ToString(), searchAttributesInProjectRequest: new SearchAttributesInProjectRequest(
                    name: "",
                    isRequired: true,
                    types: new List<CustomAttributeTypesEnum>()
                    {
                        CustomAttributeTypesEnum.String,
                        CustomAttributeTypesEnum.Options,
                        CustomAttributeTypesEnum.MultipleOptions,
                        CustomAttributeTypesEnum.User,
                        CustomAttributeTypesEnum.Datetime
                    }
                ));

            var requiredAttributes = attributes
                .Select(a => new TmsAttribute
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

            _logger.LogDebug("Got required project attributes by project id {id}: {@Attributes}", projectId, requiredAttributes);

            return requiredAttributes;
        }
        catch (Exception e)
        {
            _logger.LogError("Could not get required project attributes by project id {Id}: {Message}", projectId, e.Message);
            throw;
        }
    }

    public async Task<TmsAttribute> GetProjectAttributeById(Guid id)
    {
        _logger.LogInformation("Getting project attribute by id {Id}", id);

        try
        {
            var attribute = await _customAttributes.ApiV2CustomAttributesIdGetAsync(id);

            var customAttribute = new TmsAttribute
                {
                    Id = attribute.Id,
                    Name = attribute.Name,
                    Type = attribute.Type.ToString(),
                    IsEnabled = attribute.IsEnabled,
                    IsRequired = attribute.IsRequired,
                    IsGlobal = attribute.IsGlobal,
                    Options = attribute.Options.Select(o => new TmsAttributeOptions
                    {
                        Id = o.Id,
                        Value = o.Value,
                        IsDefault = o.IsDefault
                    }).ToList()
                };

            _logger.LogDebug("Got project attribute by id {id}: {@Attribute}", id, customAttribute);

            return customAttribute;
        }
        catch (Exception e)
        {
            _logger.LogError("Could not get project attribute by id {Id}: {Message}", id, e.Message);
            throw;
        }
    }

    public async Task AddAttributesToProject(Guid projectId, IEnumerable<Guid> attributeIds)
    {
        _logger.LogInformation("Adding attributes to project");

        try
        {
            await _projectsApi.AddGlobaAttributesToProjectAsync(projectId.ToString(), attributeIds.ToList());
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
            var model = new ApiV2CustomAttributesGlobalIdPutRequest(name: attribute.Name)
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
                apiV2CustomAttributesGlobalIdPutRequest: model);

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

    public async Task UpdateProjectAttribute(Guid projectId, TmsAttribute attribute)
    {
        _logger.LogInformation("Updating project attribute {Name}", attribute.Name);

        try
        {
            var model = new UpdateProjectsAttributeRequest(id: attribute.Id, name: attribute.Name)
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

            await _projectAttributesApi.UpdateProjectsAttributeAsync(
                projectId: projectId.ToString(), updateProjectsAttributeRequest: model);
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
            var model = new CreateParameterRequest(name: parameter.Name,
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
                apiV2ParametersSearchPostRequest: new ApiV2ParametersSearchPostRequest(name: name, isDeleted: false));

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
