using System.Text.Json.Serialization;

namespace Youlai.Application.Form.Models;

/// <summary>
/// 表单定义表单对象
/// </summary>
public sealed class FormDefinitionForm
{
    /// <summary>表单ID</summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>表单唯一标识（创建后不可修改，小写字母开头，仅含小写字母、数字、下划线）</summary>
    [JsonPropertyName("formKey")]
    public string FormKey { get; init; } = string.Empty;

    /// <summary>表单名称</summary>
    [JsonPropertyName("formName")]
    public string FormName { get; init; } = string.Empty;

    /// <summary>表单描述</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>表单规则（form-create rule 数组 JSON 字符串，由设计器产出）</summary>
    [JsonPropertyName("formJson")]
    public string? FormJson { get; init; }

    /// <summary>表单全局配置 JSON 字符串</summary>
    [JsonPropertyName("optionsJson")]
    public string? OptionsJson { get; init; }

    /// <summary>是否允许匿名公开访问 0否 1是</summary>
    [JsonPropertyName("isPublic")]
    public int? IsPublic { get; init; }

    /// <summary>表单类型 normal通用表单 workflow工作流表单</summary>
    [JsonPropertyName("category")]
    public string? Category { get; init; }
}
