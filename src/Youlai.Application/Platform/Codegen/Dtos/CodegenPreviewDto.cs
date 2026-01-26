using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 代码预览条目
/// </summary>
public sealed class CodegenPreviewDto
{
    /// <summary>
    /// 文件路径
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// 文件内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}
