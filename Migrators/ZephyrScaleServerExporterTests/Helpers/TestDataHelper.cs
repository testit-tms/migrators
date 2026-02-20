using System.Collections.Generic;
using System.Linq;
using Models;
using NUnit.Framework.Internal;
using ZephyrScaleServerExporter.Models;
using ZephyrScaleServerExporter.Models.Attachment;
using ZephyrScaleServerExporter.Models.Client;
using ZephyrScaleServerExporter.Models.Common;
using ZephyrScaleServerExporter.Models.TestCases;
using ZephyrScaleServerExporter.Models.TestCases.Export;
using Attribute = Models.Attribute;
using TestCaseData = ZephyrScaleServerExporter.Models.TestCases.TestCaseData;
using ZephyrConstants = ZephyrScaleServerExporter.Models.Common.Constants;

namespace ZephyrScaleServerExporterTests.Helpers;

public static class TestDataHelper
{
    private static readonly Randomizer Randomizer = Randomizer.CreateRandomizer();

    public static int GenerateProjectId(int min = 10000, int max = 99999)
    {
        return Randomizer.Next(min, max);
    }

    public static List<ZephyrTestCase> CreateTestZephyrTestCases(int count)
    {
        return Enumerable.Range(1, count).Select(i => new ZephyrTestCase
        {
            Key = $"TEST-{i}",
            Name = $"Test Case {i}",
            Description = $"Description for test case {i}",
            Parameters = new Dictionary<string, object>()
        }).ToList();
    }

    public static TestCaseData CreateTestCaseData(List<Guid> testCaseIds)
    {
        return new TestCaseData
        {
            TestCaseIds = testCaseIds,
            Attributes = new List<Attribute>
            {
                new Attribute
                {
                    Id = Guid.NewGuid(),
                    Name = "Priority",
                    Type = AttributeType.Options,
                    IsRequired = true,
                    IsActive = true,
                    Options = new List<string> { "High", "Medium", "Low" }
                }
            }
        };
    }

    public static ZephyrTestCase CreateZephyrTestCase(
        string? key = null,
        string? name = null,
        string? description = null,
        string? component = null,
        string? status = null,
        string? priority = null,
        Dictionary<string, object>? customFields = null,
        string? folder = null,
        string? ownerKey = null,
        string? jiraId = null,
        bool isArchived = false)
    {
        return new ZephyrTestCase
        {
            Key = key ?? "TEST-1",
            Name = name ?? "Test Case",
            Description = description ?? "Description",
            Component = component,
            Status = status ?? "Approved",
            Priority = priority ?? "High",
            CustomFields = customFields,
            Folder = folder,
            OwnerKey = ownerKey,
            JiraId = jiraId,
            IsArchived = isArchived,
            Parameters = new Dictionary<string, object>()
        };
    }

    public static SectionData CreateSectionData(
        Guid? mainSectionId = null,
        Dictionary<string, Guid>? sectionMap = null,
        Dictionary<string, Section>? allSections = null)
    {
        var mainId = mainSectionId ?? Guid.NewGuid();
        var mainSection = new Section
        {
            Id = mainId,
            Name = "Main Section",
            PreconditionSteps = new List<Step>(),
            PostconditionSteps = new List<Step>(),
            Sections = new List<Section>()
        };

        var map = sectionMap ?? new Dictionary<string, Guid> { { ZephyrConstants.MainFolderKey, mainId } };
        var sections = allSections ?? new Dictionary<string, Section> { { ZephyrConstants.MainFolderKey, mainSection } };

        return new SectionData
        {
            MainSection = mainSection,
            SectionMap = map,
            AllSections = sections
        };
    }

    public static Dictionary<string, Attribute> CreateAttributeMap(
        bool includeComponent = true,
        bool includeRequired = true)
    {
        var map = new Dictionary<string, Attribute>();

        if (includeComponent)
        {
            map[ZephyrConstants.ComponentAttribute] = new Attribute
            {
                Id = Guid.NewGuid(),
                Name = ZephyrConstants.ComponentAttribute,
                Type = AttributeType.Options,
                IsRequired = false,
                IsActive = true,
                Options = new List<string>()
            };
        }

        map[ZephyrConstants.IdZephyrAttribute] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = ZephyrConstants.IdZephyrAttribute,
            Type = AttributeType.String,
            IsRequired = true,
            IsActive = true,
            Options = new List<string>()
        };

        map[ZephyrConstants.ZephyrStatusAttribute] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = ZephyrConstants.ZephyrStatusAttribute,
            Type = AttributeType.Options,
            IsRequired = includeRequired,
            IsActive = true,
            Options = new List<string> { "Approved", "Draft", "Deprecated" }
        };

        if (includeRequired)
        {
            map["RequiredAttribute"] = new Attribute
            {
                Id = Guid.NewGuid(),
                Name = "RequiredAttribute",
                Type = AttributeType.String,
                IsRequired = true,
                IsActive = true,
                Options = new List<string>()
            };
        }

        map["CheckboxAttribute"] = new Attribute
        {
            Id = Guid.NewGuid(),
            Name = "CheckboxAttribute",
            Type = AttributeType.Checkbox,
            IsRequired = false,
            IsActive = true,
            Options = new List<string>()
        };

        return map;
    }

    public static TraceLink CreateTraceLink(
        string? issueId = null,
        string? urlDescription = null,
        string? url = null,
        string? confluencePageId = null)
    {
        return new TraceLink
        {
            Id = Randomizer.Next(1, 10000),
            IssueId = issueId,
            UrlDescription = urlDescription,
            Url = url,
            ConfluencePageId = confluencePageId
        };
    }

    public static TraceLinksRoot CreateTraceLinksRoot(
        List<TraceLink>? traceLinks = null,
        int id = 1)
    {
        return new TraceLinksRoot
        {
            Id = id,
            TraceLinks = traceLinks ?? new List<TraceLink>()
        };
    }

    public static TestCaseTracesResponseWrapper CreateTestCaseTracesResponseWrapper(
        List<TraceLinksRoot>? results = null)
    {
        return new TestCaseTracesResponseWrapper
        {
            Results = results ?? new List<TraceLinksRoot>()
        };
    }

    public static ZephyrConfluenceLink CreateZephyrConfluenceLink(
        string? title = null,
        string? url = null)
    {
        return new ZephyrConfluenceLink
        {
            Title = title ?? "Confluence Page",
            Url = url ?? "https://confluence.example.com/page"
        };
    }

    public static JiraIssue CreateJiraIssue(
        string? key = null,
        string? id = null,
        string? name = null)
    {
        return new JiraIssue
        {
            Key = key ?? "TEST-123",
            Id = id ?? "10001",
            Url = "https://jira.example.com/rest/api/2/issue/10001",
            Fields = new JiraIssueFields
            {
                Name = name ?? "Test Issue"
            }
        };
    }

    public static ZephyrOwner CreateZephyrOwner(
        string? key = null,
        string? displayName = null)
    {
        return new ZephyrOwner
        {
            Key = key ?? "user1",
            Name = "User One",
            EmailAddress = "user1@example.com",
            DisplayName = displayName ?? "User One"
        };
    }

    public static ZephyrAttachment CreateZephyrAttachment(
        string? fileName = null,
        string? url = null)
    {
        return new ZephyrAttachment
        {
            FileName = fileName ?? "test.txt",
            Url = url ?? "/rest/attachment/1"
        };
    }

    public static ZephyrDescriptionData CreateZephyrDescriptionData(
        string? description = null,
        List<ZephyrAttachment>? attachments = null)
    {
        return new ZephyrDescriptionData
        {
            Description = description ?? "Test description",
            Attachments = attachments ?? new List<ZephyrAttachment>()
        };
    }

    public static StatusData CreateStatusData(
        string? stringStatuses = null,
        Attribute? statusAttribute = null)
    {
        return new StatusData
        {
            StringStatuses = stringStatuses ?? "\"Approved\",\"Draft\"",
            StatusAttribute = statusAttribute ?? new Attribute
            {
                Id = Guid.NewGuid(),
                Name = ZephyrConstants.ZephyrStatusAttribute,
                Type = AttributeType.Options,
                IsRequired = false,
                IsActive = true,
                Options = new List<string> { "Approved", "Draft" }
            }
        };
    }

    public static TestCaseExportRequiredModel CreateTestCaseExportRequiredModel(
        Attribute? ownersAttribute = null,
        StatusData? statusData = null,
        List<string>? requiredAttributeNames = null)
    {
        return new TestCaseExportRequiredModel
        {
            OwnersAttribute = ownersAttribute ?? new Attribute
            {
                Id = Guid.NewGuid(),
                Name = ZephyrConstants.OwnerAttribute,
                Type = AttributeType.Options,
                IsRequired = false,
                IsActive = true,
                Options = new List<string>()
            },
            StatusData = statusData ?? CreateStatusData(),
            RequiredAttributeNames = requiredAttributeNames ?? new List<string>()
        };
    }
}
