using System.Text.Json.Serialization;

namespace Youlai.Application.Form.Models;

/// <summary>
/// 表单渲染对象（渲染端按 formKey 加载的已发布规则）
/// </summary>
public sealed class FormRenderVo
{
    /// <summary>表单唯一标识</summary>
    [JsonPropertyName("formKey")]
    public string FormKey { get; init; } = string.Empty;

    /// <summary>表单名称</summary>
    [JsonPropertyName("formName")]
    public string FormName { get; init; } = string.Empty;

    /// <summary>表单版本号</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; }

    /// <summary>表单规则（form-create rule 数组 JSON 字符串）</summary>
    [JsonPropertyName("formJson")]
    public string FormJson { get; init; } = "[]";

    /// <summary>表单全局配置 JSON 字符串</summary>
    [JsonPropertyName("optionsJson")]
    public string? OptionsJson { get; init; }
}
