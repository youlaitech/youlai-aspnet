using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.Form.Models;

/// <summary>
/// AI 生成表单入参
/// </summary>
public sealed class FormAiGenerateForm
{
    /// <summary>表单需求描述</summary>
    [Required(ErrorMessage = "表单需求描述不能为空")]
    [StringLength(2000, ErrorMessage = "表单需求描述不能超过 2000 个字符")]
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}
