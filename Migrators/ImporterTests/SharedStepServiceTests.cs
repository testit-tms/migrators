using Importer.Client;
using Importer.Models;
using Importer.Services;
using Microsoft.Extensions.Logging;
using Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ImporterTests;

public class SharedStepServiceTests
{
    private ILogger<SharedStepService> _logger;
    private IParserService _parserService;
    private IClient _client;
    private IAttachmentService _attachmentService;
    private Dictionary<Guid, TmsAttribute> _attributesMap;
    private Dictionary<Guid, Guid> _sectionsMap;
    private List<Guid> _sharedStepsIds;
    private Dictionary<Guid, Guid> _sharedStepsMap;
    private List<SharedStep> _sharedSteps;
    private List<SharedStep> _sharedStepsChanged;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<SharedStepService>>();
        _parserService = Substitute.For<IParserService>();
        _client = Substitute.For<IClient>();
        _attachmentService = Substitute.For<IAttachmentService>();
        _sharedStepsIds = new List<Guid>()
        {
            Guid.Parse("cacaec23-cf89-46f8-918e-bfae7003895e"),
            Guid.Parse("ad1b46bc-13c6-400f-af4d-8243c0aec4d2")
        };
        _sectionsMap = new Dictionary<Guid, Guid>
        {
            { Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c88"), Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c10") },
            { Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781de9"), Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781d10") }
        };
        _sharedStepsMap = new Dictionary<Guid, Guid>
        {
            { Guid.Parse("cacaec23-cf89-46f8-918e-bfae7003895e"), Guid.Parse("cacaec23-cf89-46f8-918e-bfae70038910") },
            { Guid.Parse("ad1b46bc-13c6-400f-af4d-8243c0aec4d2"), Guid.Parse("ad1b46bc-13c6-400f-af4d-8243c0aec410") }
        };
        _attributesMap = new Dictionary<Guid, TmsAttribute>
        {
            {
                Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee6"),
                new TmsAttribute
                {
                    Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbe10"),
                    Name = "TestAttribute",
                    IsRequired = false,
                    IsEnabled = true,
                    Type = "String",
                    Options = new List<TmsAttributeOptions>()
                }
            },
            {
                Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad0d"),
                new TmsAttribute
                {
                    Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad10"),
                    Name = "TestAttribute2",
                    IsRequired = false,
                    IsEnabled = true,
                    Type = "Options",
                    Options = new List<TmsAttributeOptions>
                    {
                        new()
                        {
                            Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad11"),
                            Value = "Option1",
                            IsDefault = true
                        },
                        new()
                        {
                            Id = Guid.Parse("9767ce0e-a214-4ebc-af69-71aa88b0ad12"),
                            Value = "Option2",
                            IsDefault = false
                        }
                    }
                }
            }
        };

        _sharedSteps = new List<SharedStep>()
        {
            new SharedStep()
            {
                Id = Guid.Parse("cacaec23-cf89-46f8-918e-bfae7003895e"),
                Name = "TestSharedStep",
                Description = "TestSharedStepDescription",
                Steps = new List<Step>
                {
                    new()
                    {
                        Action = "TestAction",
                        Expected = "TestExpectedResult"
                    },
                },
                Attributes = new List<CaseAttribute>
                {
                    new()
                    {
                        Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee6"),
                        Value = "TestValue"
                    }
                },
                Attachments = new List<string>()
                {
                    "TestAttachment"
                },
                Priority = PriorityType.Medium,
                State = StateType.Ready,
                SectionId = Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c88"),
                Links = new List<Link>()
                {
                    new Link()
                    {
                        Description = "TestLinkDescription",
                        Type = LinkType.Defect,
                        Title = "TestLinkTitle",
                        Url = "https://ya.ru"
                    }
                },
                Tags = new List<string>()
                {
                    "TestTag"
                }
            },
            new SharedStep()
            {
                Id = Guid.Parse("ad1b46bc-13c6-400f-af4d-8243c0aec4d2"),
                Name = "TestSharedStep2",
                Description = "TestSharedStepDescription2",
                Steps = new List<Step>
                {
                    new()
                    {
                        Action = "TestAction",
                        Expected = "TestExpectedResult"
                    },
                },
                Attributes = new List<CaseAttribute>
                {
                    new()
                    {
                        Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbee6"),
                        Value = "TestValue"
                    }
                },
                Attachments = new List<string>(),
                Priority = PriorityType.Medium,
                State = StateType.Ready,
                SectionId = Guid.Parse("0993a214-1ff7-4350-bdaf-275f53781de9"),
                Links = new List<Link>(),
                Tags = new List<string>()
                {
                    "TestTag"
                }
            }
        };

        _sharedStepsChanged = new List<SharedStep>()
        {
            new SharedStep()
            {
                Id = Guid.Parse("cacaec23-cf89-46f8-918e-bfae7003895e"),
                Name = "TestSharedStep",
                Description = "TestSharedStepDescription",
                Steps = new List<Step>
                {
                    new()
                    {
                        Action = "TestAction",
                        Expected = "TestExpectedResult"
                    },
                },
                Attributes = new List<CaseAttribute>
                {
                    new()
                    {
                        Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbe10"),
                        Value = "TestValue"
                    }
                },
                Attachments = new List<string>()
                {
                    "64fd2285-7a94-5d2e-8f3e-033225b38c99"
                },
                Priority = PriorityType.Medium,
                State = StateType.Ready,
                SectionId = Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c88"),
                Links = new List<Link>()
                {
                    new Link()
                    {
                        Description = "TestLinkDescription",
                        Type = LinkType.Defect,
                        Title = "TestLinkTitle",
                        Url = "https://ya.ru"
                    }
                },
                Tags = new List<string>()
                {
                    "TestTag"
                }
            },
            new SharedStep()
            {
                Id = Guid.Parse("ad1b46bc-13c6-400f-af4d-8243c0aec4d2"),
                Name = "TestSharedStep2",
                Description = "TestSharedStepDescription2",
                Steps = new List<Step>
                {
                    new()
                    {
                        Action = "TestAction",
                        Expected = "TestExpectedResult"
                    },
                },
                Attributes = new List<CaseAttribute>
                {
                    new()
                    {
                        Id = Guid.Parse("8e2b4dc4-f6c3-472f-a58f-d57b968bbe10"),
                        Value = "TestValue"
                    }
                },
                Attachments = new List<string>(),
                Priority = PriorityType.Medium,
                State = StateType.Ready,
                SectionId = Guid.Parse("82fd2285-7a94-4d2e-8f3e-033225b38c88"),
                Links = new List<Link>(),
                Tags = new List<string>()
                {
                    "TestTag"
                }
            }
        };
    }

    [Test]
    public async Task ImportSharedSteps_FailedGetSharedStep()
    {
        // Arrange
        _parserService.GetSharedStep(_sharedStepsIds[0]).ThrowsAsync(new Exception("Failed to get shared step"));

        var service = new SharedStepService(_logger, _client, _parserService, _attachmentService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ImportSharedSteps(_sharedStepsIds, _sectionsMap, _attributesMap));

        // Assert
        await _attachmentService.DidNotReceive().GetAttachments(Arg.Any<Guid>(), Arg.Any<string[]>());
        await _client.DidNotReceive().ImportSharedStep(Arg.Any<Guid>(), Arg.Any<SharedStep>());
    }

    [Test]
    public async Task ImportSharedSteps_FailedGetAttachments()
    {
        // Arrange
        _parserService.GetSharedStep(_sharedStepsIds[0]).Returns(_sharedSteps[0]);
        _attachmentService.GetAttachments(_sharedSteps[0].Id, _sharedSteps[0].Attachments)
            .ThrowsAsync(new Exception("Failed to get attachments"));

        var service = new SharedStepService(_logger, _client, _parserService, _attachmentService);

        //Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ImportSharedSteps(_sharedStepsIds, _sectionsMap, _attributesMap));

        // Assert
        await _client.DidNotReceive().ImportSharedStep(Arg.Any<Guid>(), Arg.Any<SharedStep>());
    }

    [Test]
    public async Task ImportSharedSteps_FailedImportSharedStep()
    {
        // Arrange
        _parserService.GetSharedStep(_sharedStepsIds[0]).Returns(_sharedSteps[0]);
        _attachmentService.GetAttachments(_sharedSteps[0].Id, _sharedSteps[0].Attachments)
            .Returns(_sharedStepsChanged[0].Attachments);
        // TODO: use real model of shared step
        _client.ImportSharedStep(_sectionsMap[_sharedSteps[0].SectionId], Arg.Any<SharedStep>())
            .ThrowsAsync(new Exception("Failed to import shared step"));

        var service = new SharedStepService(_logger, _client, _parserService, _attachmentService);

        //Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.ImportSharedSteps(_sharedStepsIds, _sectionsMap, _attributesMap));
    }


    [Test]
    public async Task ImportSharedSteps_Success()
    {
        // Arrange
        _parserService.GetSharedStep(_sharedStepsIds[0]).Returns(_sharedSteps[0]);
        _parserService.GetSharedStep(_sharedStepsIds[1]).Returns(_sharedSteps[1]);
        _attachmentService.GetAttachments(_sharedSteps[0].Id, _sharedSteps[0].Attachments)
            .Returns(_sharedStepsChanged[0].Attachments);
        _attachmentService.GetAttachments(_sharedSteps[1].Id, _sharedSteps[1].Attachments)
            .Returns(_sharedStepsChanged[1].Attachments);
        // TODO: use real model of shared step
        _client.ImportSharedStep(_sectionsMap[_sharedSteps[0].SectionId], Arg.Any<SharedStep>())
            .Returns(_sharedStepsMap[_sharedSteps[0].Id]);
        _client.ImportSharedStep(_sectionsMap[_sharedSteps[1].SectionId], Arg.Any<SharedStep>())
            .Returns(_sharedStepsMap[_sharedSteps[1].Id]);

        var service = new SharedStepService(_logger, _client, _parserService, _attachmentService);

        // Act
        var resp = await service.ImportSharedSteps(_sharedStepsIds, _sectionsMap, _attributesMap);

        // Assert
        Assert.That(resp, Is.EqualTo(_sharedStepsMap));
    }
}
