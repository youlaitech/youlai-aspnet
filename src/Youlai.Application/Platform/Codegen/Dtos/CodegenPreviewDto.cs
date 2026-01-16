using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 代码预览条目
/// </summary>
public sealed class CodegenPreviewDto
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}
