using System.Text.Json.Serialization;

namespace Youlai.Application.Codegen.Models;

/// <summary>
/// AI 填充生成配置入参
/// </summary>
public sealed class AiFillConfigForm
{
    /// <summary>
    /// 补充需求描述，可为空
    /// </summary>
    [JsonPropertyName("requirement")]
    public string? Requirement { get; init; }
}
