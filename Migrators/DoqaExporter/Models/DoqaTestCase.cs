using System.Text.Json.Serialization;

namespace DoqaExporter.Models;

public class DoqaLoginResponse
{
    [JsonPropertyName("tokens")]
    public DoqaTokens Tokens { get; set; } = new();

    [JsonPropertyName("user")]
    public DoqaUser User { get; set; } = new();
}

public class DoqaTokens
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
}

public class DoqaUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
}

public class DoqaConfigResponse
{
    [JsonPropertyName("fileSystemLink")]
    public string FileSystemLink { get; set; } = string.Empty;
}

public class DoqaProjectsResponse
{
    [JsonPropertyName("projects")]
    public List<DoqaProject> Projects { get; set; } = new();
}

public class DoqaProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("spaces")]
    public List<DoqaSpace> Spaces { get; set; } = new();
}

public class DoqaSpace
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("casesCount")]
    public int CasesCount { get; set; }
}

public class DoqaCasesListResponse
{
    [JsonPropertyName("data")]
    public DoqaTreeNode? Data { get; set; }
}

public class DoqaTreeNode
{
    [JsonPropertyName("data")]
    public DoqaTreeNodeData? Data { get; set; }

    [JsonPropertyName("children")]
    public List<DoqaTreeNode> Children { get; set; } = new();
}

public class DoqaTreeNodeData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isFolder")]
    public bool IsFolder { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

public class DoqaFolderTree
{
    [JsonPropertyName("data")]
    public DoqaFolderData Data { get; set; } = new();

    [JsonPropertyName("children")]
    public List<DoqaFolderTree> Children { get; set; } = new();
}

public class DoqaFolderData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

public class DoqaTestCase
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("preconditions")]
    public string Preconditions { get; set; } = string.Empty;

    [JsonPropertyName("expectedResult")]
    public string ExpectedResult { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ready";

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    [JsonPropertyName("folderId")]
    public int FolderId { get; set; }

    [JsonPropertyName("spaceId")]
    public int SpaceId { get; set; }

    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("steps")]
    public List<DoqaStep> Steps { get; set; } = new();

    [JsonPropertyName("tagIds")]
    public List<int> TagIds { get; set; } = new();

    [JsonPropertyName("attachments")]
    public List<DoqaAttachment> Attachments { get; set; } = new();
}

public class DoqaStep
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("step")]
    public string StepText { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("testData")]
    public string? TestData { get; set; }
}

public class DoqaAttachment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("originalName")]
    public string OriginalName { get; set; } = string.Empty;
}

// -- Checklists --------------------------------------------------------

public class DoqaChecklistsListResponse
{
    [JsonPropertyName("data")]
    public DoqaTreeNode? Data { get; set; }
}

public class DoqaChecklist
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ready";

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    [JsonPropertyName("folderId")]
    public int FolderId { get; set; }

    [JsonPropertyName("spaceId")]
    public int SpaceId { get; set; }

    [JsonPropertyName("children")]
    public List<DoqaChecklistItem> Children { get; set; } = new();

    [JsonPropertyName("attachments")]
    public List<DoqaAttachment> Attachments { get; set; } = new();
}

public class DoqaChecklistItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("parentId")]
    public int? ParentId { get; set; }

    [JsonPropertyName("children")]
    public List<DoqaChecklistItem> Children { get; set; } = new();
}
