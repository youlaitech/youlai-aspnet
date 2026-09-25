using System.Text.Json.Serialization;

namespace Youlai.Application.Form.Models;

/// <summary>
/// 表单定义分页数据
/// </summary>
public sealed class FormDefinitionPageVo
{
    /// <summary>表单ID</summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>表单唯一标识</summary>
    [JsonPropertyName("formKey")]
    public string FormKey { get; init; } = string.Empty;

    /// <summary>表单名称</summary>
    [JsonPropertyName("formName")]
    public string FormName { get; init; } = string.Empty;

    /// <summary>表单描述</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>状态 0草稿 1已发布 -1已停用</summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>版本号</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; }

    /// <summary>是否允许匿名公开访问 0否 1是</summary>
    [JsonPropertyName("isPublic")]
    public int IsPublic { get; init; }

    /// <summary>表单类型 normal通用表单 workflow审批表单</summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = "normal";

    /// <summary>创建时间</summary>
    [JsonPropertyName("createTime")]
    public DateTime? CreateTime { get; init; }
}
