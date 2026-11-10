using System.ComponentModel.DataAnnotations;
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
    [StringLength(64, ErrorMessage = "表单标识不能超过 64 个字符")]
    [RegularExpression("^[a-z][a-z0-9_]*$", ErrorMessage = "表单标识只能以小写字母开头，且只含小写字母、数字和下划线")]
    [JsonPropertyName("formKey")]
    public string? FormKey { get; init; }

    /// <summary>表单名称</summary>
    [StringLength(100, ErrorMessage = "表单名称不能超过 100 个字符")]
    [JsonPropertyName("formName")]
    public string? FormName { get; init; }

    /// <summary>表单描述</summary>
    [StringLength(255, ErrorMessage = "表单描述不能超过 255 个字符")]
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
    [RegularExpression("^(normal|workflow)$", ErrorMessage = "表单类型只能是 normal 或 workflow")]
    [JsonPropertyName("category")]
    public string? Category { get; init; }
}
