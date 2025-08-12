using Importer.Client;
using Importer.Client.Implementations;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using NSubstitute;
using TestIT.ApiClient.Api;
using TestIT.ApiClient.Client;
using TestIT.ApiClient.Model;
using Attribute = Models.Attribute;

namespace ImporterTests;

[TestFixture]
public class ClientAdapterTests
{
    private ILogger<ClientAdapter> _logger = null!;
    private IOptions<AppConfig> _appConfig = null!;
    private IAttachmentsApi _attachmentsApi = null!;
    private IProjectsApi _projectsApi = null!;
    private IProjectAttributesApi _projectAttributesApi = null!;
    private IProjectSectionsApi _projectSectionsApi = null!;
    private ISectionsApi _sectionsApi = null!;
    private ICustomAttributesApi _customAttributesApi = null!;
    private IWorkItemsApi _workItemsApi = null!;
    private IParametersApi _parametersApi = null!;
    private ClientAdapter _clientAdapter = null!;
    private IAdapterHelper _adapterHelper = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ClientAdapter>>();
        _appConfig = Substitute.For<IOptions<AppConfig>>();
        _attachmentsApi = Substitute.For<IAttachmentsApi>();
        _projectsApi = Substitute.For<IProjectsApi>();
        _projectAttributesApi = Substitute.For<IProjectAttributesApi>();
        _projectSectionsApi = Substitute.For<IProjectSectionsApi>();
        _sectionsApi = Substitute.For<ISectionsApi>();
        _customAttributesApi = Substitute.For<ICustomAttributesApi>();
        _workItemsApi = Substitute.For<IWorkItemsApi>();
        _parametersApi = Substitute.For<IParametersApi>();
        _adapterHelper = Substitute.For<IAdapterHelper>();

        _appConfig.Value.Returns(new AppConfig
        {
            Tms = new TmsConfig
            {
                ImportToExistingProject = false
            }
        });

        _clientAdapter = new ClientAdapter(
            _logger,
            _appConfig,
            _adapterHelper,
            _attachmentsApi,
            _projectsApi,
            _projectAttributesApi,
            _projectSectionsApi,
            _sectionsApi,
            _customAttributesApi,
            _workItemsApi,
            _parametersApi
        );
    }

    [TearDown]
    public void TearDown()
    {
        if (_projectAttributesApi is IDisposable disposableProjectAttributesApi)
            disposableProjectAttributesApi.Dispose();

        if (_projectSectionsApi is IDisposable disposableProjectSectionsApi)
            disposableProjectSectionsApi.Dispose();

        if (_workItemsApi is IDisposable disposableWorkItemsApi)
            disposableWorkItemsApi.Dispose();

        if (_parametersApi is IDisposable disposableParametersApi)
            disposableParametersApi.Dispose();
    }

    [Test]
    public async Task GetProject_WhenProjectExists_AndImportToExistingProjectFalse_ThrowsException()
    {
        // Arrange
        var projectName = "TestProject";
        var projectId = Guid.NewGuid();
        var createdById = Guid.NewGuid();
        var projects = new List<ProjectShortModel>
        {
            new(
                id: projectId,
                description: string.Empty,
                name: projectName,
                isFavorite: false,
                testCasesCount: 0,
                sharedStepsCount: 0,
                checkListsCount: 0,
                autoTestsCount: 0,
                isDeleted: false,
                createdDate: DateTime.UtcNow,
                modifiedDate: null,
                createdById: createdById,
                modifiedById: null,
                globalId: 1,
                type: new ProjectTypeModel()
            )
        };

        _projectsApi.ApiV2ProjectsSearchPostAsync(
            (int?)null, (int?)null, null, null, null,
            Arg.Any<ProjectsFilterModel>(),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(projects));

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () => await _clientAdapter.GetProject(projectName));
        Assert.That(exception.Message, Is.EqualTo("Project with the same name already exists"));
    }

    [Test]
    public async Task GetProject_WhenProjectExists_AndImportToExistingProjectTrue_ReturnsProjectId()
    {
        // Arrange
        var projectName = "TestProject";
        var projectId = Guid.NewGuid();
        var createdById = Guid.NewGuid();
        var projects = new List<ProjectShortModel>
        {
            new(
                id: projectId,
                description: string.Empty,
                name: projectName,
                isFavorite: false,
                testCasesCount: 0,
                sharedStepsCount: 0,
                checkListsCount: 0,
                autoTestsCount: 0,
                isDeleted: false,
                createdDate: DateTime.UtcNow,
                modifiedDate: null,
                createdById: createdById,
                modifiedById: null,
                globalId: 1,
                type: new ProjectTypeModel()
            )
        };

        _appConfig.Value.Returns(new AppConfig
        {
            Tms = new TmsConfig
            {
                ImportToExistingProject = true
            }
        });

        _projectsApi.ApiV2ProjectsSearchPostAsync(
            (int?)null, (int?)null, null, null, null,
            Arg.Any<ProjectsFilterModel>(),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(projects));

        // Act
        var result = await _clientAdapter.GetProject(projectName);

        // Assert
        Assert.That(result, Is.EqualTo(projectId));
    }

    [Test]
    public async Task CreateProject_WhenSuccessful_ReturnsProjectId()
    {
        // Arrange
        var projectName = "TestProject";
        var projectId = Guid.NewGuid();
        var createdById = Guid.NewGuid();
        var projectModel = new ProjectModel(
            id: projectId,
            description: string.Empty,
            name: projectName,
            isFavorite: false,
            attributesScheme: new List<CustomAttributeModel>(),
            testPlansAttributesScheme: new List<CustomAttributeModel>(),
            testCasesCount: 0,
            sharedStepsCount: 0,
            checkListsCount: 0,
            autoTestsCount: 0,
            isDeleted: false,
            createdDate: DateTime.UtcNow,
            modifiedDate: null,
            createdById: createdById,
            modifiedById: null,
            globalId: 1,
            type: new ProjectTypeModel());

        _projectsApi.CreateProjectAsync(Arg.Any<CreateProjectApiModel>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projectModel));

        // Act
        var result = await _clientAdapter.CreateProject(projectName);

        // Assert
        Assert.That(result, Is.EqualTo(projectId));
        await _projectsApi.Received(1).CreateProjectAsync(Arg.Is<CreateProjectApiModel>(r => r.Name == projectName), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ImportAttribute_WhenSuccessful_ReturnsTmsAttribute()
    {
        // Arrange
        var attributeName = "TestAttribute";
        var attributeId = Guid.NewGuid();
        var attribute = new Attribute
        {
            Name = attributeName,
            Type = AttributeType.String,
            IsRequired = true,
            IsActive = true
        };

        var customAttributeModel = new CustomAttributeModel(
            id: attributeId,
            options: new List<CustomAttributeOptionModel>(),
            type: CustomAttributeTypesEnum.String,
            isDeleted: false,
            name: attributeName,
            isEnabled: true,
            isRequired: true,
            isGlobal: true
        );

        _customAttributesApi
            .ApiV2CustomAttributesGlobalPostAsync(
                Arg.Is<GlobalCustomAttributePostModel>(
                    r => r.Name == attributeName &&
                         r.Type == CustomAttributeTypesEnum.String &&
                         r.IsRequired == true &&
                         r.IsEnabled == true))
            .Returns(Task.FromResult(customAttributeModel));

        // Act
        var result = await _clientAdapter.ImportAttribute(attribute);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(attributeId));
            Assert.That(result.Name, Is.EqualTo(attributeName));
            Assert.That(result.Type, Is.EqualTo(CustomAttributeTypesEnum.String.ToString()));
            Assert.That(result.IsRequired, Is.EqualTo(true));
            Assert.That(result.IsEnabled, Is.EqualTo(true));
        });
    }

    [Test]
    public async Task ImportSection_WhenSuccessful_ReturnsSectionId()
    {
        // Arrange
        var sectionName = "TestSection";
        var projectId = Guid.NewGuid();
        var parentSectionId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var section = new Section
        {
            Name = sectionName,
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>()
        };

        var sectionModel = new SectionWithStepsModel(
            id: sectionId,
            name: sectionName,
            createdById: Guid.NewGuid(),
            createdDate: DateTime.UtcNow,
            attachments: new List<AttachmentModel>(),
            preconditionSteps: new List<StepModel>(),
            postconditionSteps: new List<StepModel>(),
            projectId: projectId,
            parentId: parentSectionId,
            isDeleted: false,
            modifiedDate: null,
            modifiedById: null
        );

        _sectionsApi.CreateSectionAsync(Arg.Any<SectionPostModel>())
            .Returns(Task.FromResult(sectionModel));

        // Act
        var result = await _clientAdapter.ImportSection(projectId, parentSectionId, section);

        // Assert
        Assert.That(result, Is.EqualTo(sectionId));
        await _sectionsApi.Received(1).CreateSectionAsync(
            Arg.Is<SectionPostModel>(r =>
                r.Name == sectionName &&
                r.ProjectId == projectId &&
                r.ParentId == parentSectionId
            )
        );
    }

    [Test]
    public async Task GetProjectAttributes_WhenSuccessful_ReturnsTmsAttributes()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attributeName = "TestAttribute";
        var attributes = new List<CustomAttributeSearchResponseModel>
        {
            new(
                id: attributeId,
                options: new List<CustomAttributeOptionModel>(),
                type: CustomAttributeTypesEnum.String,
                isDeleted: false,
                name: attributeName,
                isEnabled: true,
                isRequired: true,
                isGlobal: true
            )
        };

        _customAttributesApi
            .ApiV2CustomAttributesSearchPostAsync(
                customAttributeSearchQueryModel: Arg.Is<CustomAttributeSearchQueryModel>(
                    r => r.IsGlobal == true && r.IsDeleted == false))
            .Returns(Task.FromResult(attributes));

        // Act
        var result = await _clientAdapter.GetProjectAttributes();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(attributeId));
            Assert.That(result[0].Name, Is.EqualTo(attributeName));
            Assert.That(result[0].Type, Is.EqualTo(CustomAttributeTypesEnum.String.ToString()));
            Assert.That(result[0].IsEnabled, Is.True);
            Assert.That(result[0].IsRequired, Is.True);
            Assert.That(result[0].IsGlobal, Is.True);
        });
    }

    [Test]
    public async Task UploadAttachment_WhenSuccessful_ReturnsAttachmentId()
    {
        // Arrange
        var fileName = "test.txt";
        var attachmentId = Guid.NewGuid();
        var content = new MemoryStream();
        var attachmentModel = new AttachmentModel(
            fileId: "test-file-id",
            type: "text/plain",
            size: 0,
            createdDate: DateTime.UtcNow,
            modifiedDate: null,
            createdById: Guid.NewGuid(),
            modifiedById: null,
            name: fileName,
            id: attachmentId
        );

        _attachmentsApi.ApiV2AttachmentsPostAsync(
            Arg.Any<FileParameter>(),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(attachmentModel));

        // Act
        var result = await _clientAdapter.UploadAttachment(fileName, content);

        // Assert
        Assert.That(result, Is.EqualTo(attachmentId));
        await _attachmentsApi.Received(1).ApiV2AttachmentsPostAsync(
            Arg.Any<FileParameter>(),
            Arg.Any<CancellationToken>()
        );
    }
}
