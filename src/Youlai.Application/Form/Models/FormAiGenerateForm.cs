using System.Text.Json.Serialization;

namespace Youlai.Application.Form.Models;

/// <summary>
/// AI 生成表单入参
/// </summary>
public sealed class FormAiGenerateForm
{
    /// <summary>表单需求描述</summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}
