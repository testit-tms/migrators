using Importer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using TestIT.ApiClient.Api;
using TestIT.ApiClient.Client;
using TestIT.ApiClient.Model;
using Attribute = Models.Attribute;
using LinkType = TestIT.ApiClient.Model.LinkType;

namespace Importer.Client.Implementations;

public class ClientAdapter(
    ILogger<ClientAdapter> logger,
    IOptions<AppConfig> appConfig,
    IAdapterHelper adapterHelper,
    IAttachmentsApi attachmentsApi,
    IProjectsApi projectsApi,
    IProjectAttributesApi projectAttributesApi,
    IProjectSectionsApi projectSectionsApi,
    ISectionsApi sectionsApi,
    ICustomAttributesApi customAttributesApi,
    IWorkItemsApi workItemsApi,
    IParametersApi parametersApi
) : IClientAdapter
{
    private const int TenMinutes = 60000;


    public async Task<Guid> GetProject(string name)
    {
        if (!string.IsNullOrEmpty(appConfig.Value.Tms.ProjectName))
        {
            logger.LogInformation("Import by custom project name {Name}",
                appConfig.Value.Tms.ProjectName);
            name = appConfig.Value.Tms.ProjectName;
        }

        logger.LogInformation("Getting project {Name}", name);

        try
        {
            var projects = await
                projectsApi.ApiV2ProjectsSearchPostAsync(
                    null, null, null!, null!, null!,
                    new ProjectsFilterModel(name));

            logger.LogDebug("Got projects {@Project} by name {Name}", projects, name);

            if (projects.Count != 0)
                foreach (var project in projects)
                    if (project.Name == name)
                    {
                        logger.LogInformation("Got project {Name} with id {Id}", project.Name, project.Id);

                        if (!appConfig.Value.Tms.ImportToExistingProject)
                            throw new Exception("Project with the same name already exists");

                        return project.Id;
                    }

            return Guid.Empty;
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            logger.LogError(e.StackTrace);
            logger.LogError("Project {Name}: {Message}", name, e.Message);
            throw;
        }
    }

    public async Task<Guid> CreateProject(string name)
    {
        if (!string.IsNullOrEmpty(appConfig.Value.Tms.ProjectName)) name = appConfig.Value.Tms.ProjectName;

        logger.LogInformation("Creating project {Name}", name);

        try
        {
            var resp = await projectsApi.CreateProjectAsync(new CreateProjectApiModel(name: name));

            logger.LogDebug("Created project {@Project}", resp);
            logger.LogInformation("Created project {Name} with id {Id}", name, resp.Id);

            return resp.Id;
        }
        catch (Exception e)
        {
            logger.LogError("Could not create project {Name}: {Message}", name, e.Message);
            throw;
        }
    }

    public async Task<Guid> ImportSection(Guid projectId, Guid parentSectionId, Section section)
    {
        logger.LogInformation("Importing section {Name}", section.Name);

        try
        {
            var model = new SectionPostModel(
                section.Name, parentId: parentSectionId, projectId: projectId, attachments: [])
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

            logger.LogDebug("Importing section {@Section}", model);

            var resp = await sectionsApi.CreateSectionAsync(model);

            logger.LogDebug("Imported section {@Section}", resp);
            logger.LogInformation("Imported section {Name} with id {Id}", section.Name, resp.Id);

            return resp.Id;
        }
        catch (Exception e)
        {
            logger.LogError("Could not import section {Name}: {Message}", section.Name, e.Message);
            throw;
        }
    }

    public async Task<TmsAttribute> ImportAttribute(Attribute attribute)
    {
        logger.LogInformation("Importing attribute {Name}", attribute.Name);

        try
        {
            var model = new GlobalCustomAttributePostModel(attribute.Name)
            {
                Type = Enum.Parse<CustomAttributeTypesEnum>(attribute.Type.ToString()),
                IsRequired = attribute.IsRequired,
                IsEnabled = attribute.IsActive,
                Options = attribute.Options.Select(o => new CustomAttributeOptionPostModel(o)).ToList()
            };
            if (model.Options.Count == 0 && (
                    model.Type == CustomAttributeTypesEnum.Options
                    || model.Type == CustomAttributeTypesEnum.MultipleOptions
                ))
                model.Options.Add(new CustomAttributeOptionPostModel("null"));

            logger.LogDebug("Importing attribute {@Attribute}", model);

            var resp = await customAttributesApi.ApiV2CustomAttributesGlobalPostAsync(model);

            logger.LogDebug("Imported attribute {@Attribute}", resp);
            logger.LogInformation("Imported attribute {Name} with id {Id}", attribute.Name, resp.Id);

            return new TmsAttribute
            {
                Id = resp.Id,
                Name = resp.Name,
                Type = resp.Type.ToString(),
                IsRequired = resp.IsRequired,
                IsEnabled = resp.IsEnabled,
                Options = resp.Options.Select(o => new TmsAttributeOptions
                {
                    Id = o.Id,
                    Value = o.Value,
                    IsDefault = o.IsDefault
                }).ToList()
            };
        }
        catch (Exception e)
        {
            logger.LogError("Could not import attribute {Name}: {Message}", attribute.Name, e.Message);
            throw;
        }
    }

    public async Task<TmsAttribute> GetAttribute(Guid id)
    {
        logger.LogInformation("Getting attribute {Id}", id);

        try
        {
            var resp = await customAttributesApi.ApiV2CustomAttributesIdGetAsync(id);

            logger.LogDebug("Got attribute {@Attribute}", resp);

            return new TmsAttribute
            {
                Id = resp.Id,
                Name = resp.Name,
                Type = resp.Type.ToString(),
                IsRequired = resp.IsRequired,
                IsEnabled = resp.IsEnabled,
                Options = resp.Options.Select(o => new TmsAttributeOptions
                {
                    Id = o.Id,
                    Value = o.Value,
                    IsDefault = o.IsDefault
                }).ToList()
            };
        }
        catch (Exception e)
        {
            logger.LogError("Could not get attribute {Id}: {Message}", id, e.Message);
            throw;
        }
    }

    public async Task<Guid> ImportSharedStep(Guid projectId, Guid parentSectionId, SharedStep sharedStep)
    {
        try
        {
            var model = new CreateWorkItemApiModel(
                steps: new List<CreateStepApiModel>(),
                preconditionSteps: new List<CreateStepApiModel>(),
                postconditionSteps: new List<CreateStepApiModel>(),
                attributes: new Dictionary<string, object>(),
                links: new List<CreateLinkApiModel>(),
                tags: new List<TagModel>(),
                name: sharedStep.Name)
            {
                EntityTypeName = WorkItemEntityTypes.SharedSteps,
                Description = sharedStep.Description,
                SectionId = parentSectionId,
                State = Enum.Parse<WorkItemStates>(sharedStep.State.ToString()),
                Priority = Enum.Parse<WorkItemPriorityModel>(sharedStep.Priority.ToString()),
                Steps = sharedStep.Steps.Select(s =>
                    new CreateStepApiModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                Attributes = sharedStep.Attributes
                    .ToDictionary(a => a.Id.ToString(),
                        a => a.Value),
                Tags = sharedStep.Tags.Select(t => new TagModel(t)).ToList(),
                Links = sharedStep.Links.Select(l =>
                    new CreateLinkApiModel(url: l.Url)
                    {
                        Title = l.Title,
                        Description = l.Description,
                        Type = Enum.Parse<LinkType>(l.Type.ToString())
                    }).ToList(),
                Name = sharedStep.Name,
                ProjectId = projectId,
                Attachments = sharedStep.Attachments.Select(a => new AssignAttachmentApiModel(Guid.Parse(a))).ToList()
            };

            logger.LogDebug("Importing shared step {Name} and {@Model}", sharedStep.Name, model);

            var resp = await workItemsApi.CreateWorkItemAsync(model);

            logger.LogDebug("Imported shared step {@SharedStep}", resp);

            logger.LogInformation("Imported shared step {Name} with id {Id}", sharedStep.Name, resp.Id);

            return resp.Id;
        }
        catch (Exception e)
        {
            logger.LogError("Could not import shared step {Name}: {Message}", sharedStep.Name, e.Message);
            throw;
        }
    }



    public async Task<bool> ImportTestCase(Guid projectId, Guid parentSectionId, TmsTestCase testCase)
    {
        logger.LogInformation("Importing test case {Name}", testCase.Name);


        try
        {
            var attributes = testCase.Attributes
                .GroupBy(attr => attr.Id)
                .Select(group =>
                {
                    // Filter values
                    var validValues = group
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        .Where(attr => attr.Value != null &&
                                       !(attr.Value is string str && string.IsNullOrEmpty(str)))
                        .Select(attr => attr.Value)
                        .ToList();


                    // If there are some valid values - take first
                    // else return null for filtering
                    var finalValue = validValues.Count > 0 ? validValues[0] : null;

                    return new
                    {
                        Id = group.Key,
                        Value = finalValue
                    };
                })
                // delete nulls
                .Where(item => item.Value != null)
                .ToDictionary(
                    item => item.Id.ToString(),
                    item => item.Value!
                );


            var model = new CreateWorkItemApiModel(
                steps: new List<CreateStepApiModel>(),
                preconditionSteps: new List<CreateStepApiModel>(),
                postconditionSteps: new List<CreateStepApiModel>(),
                attributes: new Dictionary<string, object>(),
                links: new List<CreateLinkApiModel>(),
                tags: new List<TagModel>(),
                name: testCase.Name)
            {
                EntityTypeName = WorkItemEntityTypes.TestCases,
                SectionId = parentSectionId,
                State = Enum.Parse<WorkItemStates>(testCase.State.ToString()),
                Priority = Enum.Parse<WorkItemPriorityModel>(testCase.Priority.ToString()),
                PreconditionSteps = testCase.PreconditionSteps.Select(s =>
                    new CreateStepApiModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                PostconditionSteps = testCase.PostconditionSteps.Select(s =>
                    new CreateStepApiModel
                    {
                        Action = s.Action,
                        Expected = s.Expected
                    }).ToList(),
                Steps = testCase.Steps.Select(s =>
                    new CreateStepApiModel
                    {
                        Action = s.Action,
                        Expected = s.Expected,
                        WorkItemId = s.SharedStepId,
                        TestData = s.TestData
                    }).ToList(),
                Attributes = attributes,
                Tags = testCase.Tags.Select(t => new TagModel(t)).ToList(),
                Links = testCase.Links.Select(l =>
                    new CreateLinkApiModel(url: l.Url)
                    {
                        Title = l.Title,
                        Description = l.Description,
                        Type = Enum.Parse<LinkType>(l.Type.ToString())
                    }).ToList(),
                Name = testCase.Name,
                ProjectId = projectId,
                Attachments = testCase.Attachments.Select(a => new AssignAttachmentApiModel(Guid.Parse(a))).ToList(),
                Iterations = testCase.TmsIterations.Select(i =>
                {
                    var parameters = i.Parameters.Select(p => new ParameterIterationModel(p)).ToList();
                    return new AssignIterationApiModel(parameters);
                }).ToList(),
                Duration = testCase.Duration == 0 ? TenMinutes : testCase.Duration,
                Description = testCase.Description
            };

            logger.LogDebug("Importing test case {Name} and {@Model}", testCase.Name, model);

            var response = await adapterHelper.RetryCaller(
                async () => await workItemsApi.CreateWorkItemAsync(model));

            logger.LogDebug("Imported test case {@TestCase}", response);

            logger.LogInformation("Imported test case {Name} with id {Id}", testCase.Name, response.Id);
        }
        catch (Exception e)
        {
            logger.LogError("Could not import test case {Name}: {Message}: {InnerMessage}; {Stack}",
                testCase.Name, e.Message, e.InnerException?.Message, e.StackTrace);
            throw;
        }

        return true;
    }



    public async Task<Guid> GetRootSectionId(Guid projectId)
    {
        logger.LogInformation("Getting root section id");

        try
        {
            var section = await projectSectionsApi.GetSectionsByProjectIdAsync(projectId.ToString());

            logger.LogDebug("Got root section {@Section}", section.First());

            return section.First().Id;
        }
        catch (Exception e)
        {
            logger.LogError("Could not get root section id: {Message}", e.Message);
            throw;
        }
    }

    public async Task<List<TmsAttribute>> GetProjectAttributes()
    {
        logger.LogInformation("Getting project attributes");

        try
        {
            var attributes = await customAttributesApi.ApiV2CustomAttributesSearchPostAsync(
                customAttributeSearchQueryModel: new CustomAttributeSearchQueryModel(isGlobal: true,
                    isDeleted: false));

            logger.LogDebug("Got project attributes {@Attributes}", attributes);

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
            logger.LogError("Could not get project attributes: {Message}", e.Message);
            throw;
        }
    }

    public async Task<List<TmsAttribute>> GetRequiredProjectAttributesByProjectId(Guid projectId)
    {
        logger.LogInformation("Getting required project attributes by project id {Id}", projectId);

        try
        {
            var attributes = await projectAttributesApi.SearchAttributesInProjectAsync(
                projectId.ToString(), projectAttributesFilterModel: new ProjectAttributesFilterModel(
                    "",
                    true,
                    types: new List<CustomAttributeTypesEnum>
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

            logger.LogDebug("Got required project attributes by project id {id}: {@Attributes}", projectId,
                requiredAttributes);

            return requiredAttributes;
        }
        catch (Exception e)
        {
            logger.LogError("Could not get required project attributes by project id {Id}: {Message}", projectId,
                e.Message);
            throw;
        }
    }

    public async Task<TmsAttribute> GetProjectAttributeById(Guid id)
    {
        logger.LogInformation("Getting project attribute by id {Id}", id);

        try
        {
            var attribute = await customAttributesApi.ApiV2CustomAttributesIdGetAsync(id);

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

            logger.LogDebug("Got project attribute by id {id}: {@Attribute}", id, customAttribute);

            return customAttribute;
        }
        catch (Exception e)
        {
            logger.LogError("Could not get project attribute by id {Id}: {Message}", id, e.Message);
            throw;
        }
    }

    public async Task AddAttributesToProject(Guid projectId, IEnumerable<Guid> attributeIds)
    {
        logger.LogInformation("Adding attributes to project");

        try
        {
            await projectsApi.AddGlobaAttributesToProjectAsync(projectId.ToString(),
                attributeIds.ToList());
        }
        catch (Exception e)
        {
            logger.LogError("Could not add attributes to project: {Message}", e.Message);
            throw;
        }
    }


    public async Task<TmsAttribute> UpdateAttribute(TmsAttribute attribute)
    {
        logger.LogInformation("Updating attribute {Name}", attribute.Name);

        try
        {
            var model = new GlobalCustomAttributeUpdateModel(attribute.Name)
            {
                IsEnabled = attribute.IsEnabled,
                IsRequired = attribute.IsRequired,
                Options = attribute.Options.Select(o => new CustomAttributeOptionModel
                {
                    Id = o.Id,
                    Value = o.Value,
                    IsDefault = o.IsDefault
                }).ToList()
            };

            logger.LogDebug("Updating attribute {@Model}", model);

            var resp = await customAttributesApi
                .ApiV2CustomAttributesGlobalIdPutAsync(attribute.Id,
                    model);

            logger.LogDebug("Updated attribute {@Response}", resp);

            attribute.Options = resp.Options.Select(o => new TmsAttributeOptions
            {
                Id = o.Id,
                Value = o.Value,
                IsDefault = o.IsDefault
            }).ToList();

            return attribute;
        }

        catch (Exception e)
        {
            logger.LogError(e.StackTrace);
            logger.LogError(e.ToString());
            logger.LogError("Could not update attribute {Name}: {Message}", attribute.Name, e.Message);
            throw;
        }
    }

    public async Task UpdateProjectAttribute(Guid projectId, TmsAttribute attribute)
    {
        logger.LogInformation("Updating project attribute {Name}", attribute.Name);

        try
        {
            var model = new CustomAttributePutModel(attribute.Id, name: attribute.Name)
            {
                IsEnabled = attribute.IsEnabled,
                IsRequired = attribute.IsRequired,
                Options = attribute.Options.Select(o => new CustomAttributeOptionModel
                {
                    Id = o.Id,
                    Value = o.Value,
                    IsDefault = o.IsDefault
                }).ToList()
            };

            logger.LogDebug("Updating attribute {@Model}", model);

            await projectAttributesApi.UpdateProjectsAttributeAsync(
                projectId.ToString(), model);
        }

        catch (Exception e)
        {
            logger.LogError(e.StackTrace);
            logger.LogError(e.ToString());
            logger.LogError("Could not update attribute {Name}: {Message}", attribute.Name, e.Message);
            throw;
        }
    }

    public async Task<Guid> UploadAttachment(string fileName, Stream content)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentNullException(nameof(fileName));

        if (content == null)
            throw new ArgumentNullException(nameof(content));

        logger.LogDebug("Uploading attachment {Name}", fileName);

        try
        {
            var response = await adapterHelper.RetryCaller(
                async () => await attachmentsApi.ApiV2AttachmentsPostAsync(
                    new FileParameter(
                        Path.GetFileName(fileName),
                        content: content,
                        contentType: "application/octet-stream")));

            logger.LogDebug("Uploaded attachment {@Response}", response);

            return response!.Id;
        }
        catch (Exception e)
        {
            logger.LogError("Could not upload attachment {Name}: {Message}: {Inner}, {StackTrace}", fileName,
                e.Message, e.InnerException?.Message, e.StackTrace);
            throw;
        }
    }

    public async Task<TmsParameter> CreateParameter(Parameter parameter)
    {
        logger.LogInformation("Creating parameter {Name}", parameter.Name);

        try
        {
            // check parameter and if "" change to N/A
            if (parameter.Value == null || parameter.Value.Trim() == string.Empty)
            {
                parameter.Value = "N/A";
            }
            var model = new CreateParameterApiModel(name: parameter.Name,
                value: parameter.Value);

            logger.LogDebug("Creating parameter {@Model}", model);

            var resp = await parametersApi.CreateParameterAsync(model);

            logger.LogDebug("Created parameter {@Response}", resp);

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
            logger.LogError("Could not create parameter {Name}: {Message}", parameter.Name, e.Message);
            throw;
        }
    }

    public async Task<List<TmsParameter>> GetParameter(string name)
    {
        logger.LogInformation("Getting parameter {Name}", name);

        try
        {
            var resp = await parametersApi.ApiV2ParametersSearchPostAsync(
                parametersFilterApiModel:
                new ParametersFilterApiModel(name: name, isDeleted: false));


            logger.LogDebug("Got parameter {@Response}", resp);

            return resp.Select(p =>
                    new TmsParameter
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
            logger.LogError("Could not get parameter {Name}: {Message}", name, e.Message);
            throw;
        }
    }

    public async Task<Guid> GetSection(Guid projectId, Guid parentSectionId, Section section)
    {
        logger.LogInformation("Importing section {Name}", section.Name);

        try
        {
            var model = new SectionPostModel(
                section.Name, parentId: parentSectionId, projectId: projectId)
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

            logger.LogDebug("Importing section {@Section}", model);

            var resp = await sectionsApi.CreateSectionAsync(model);

            logger.LogDebug("Imported section {@Section}", resp);
            logger.LogInformation("Imported section {Name} with id {Id}", section.Name, resp.Id);

            return resp.Id;
        }
        catch (Exception e)
        {
            logger.LogError("Could not import section {Name}: {Message}", section.Name, e.Message);
            throw;
        }
    }
}
