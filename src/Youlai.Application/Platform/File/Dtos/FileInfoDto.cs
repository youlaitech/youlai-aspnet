using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.File.Dtos;

/// <summary>
/// 文件信息
/// </summary>
public sealed class FileInfoDto
{
    /// <summary>
    /// 文件名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 访问地址
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
