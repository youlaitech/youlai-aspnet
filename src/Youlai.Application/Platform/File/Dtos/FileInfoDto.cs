using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.File.Dtos;

/// <summary>
/// 文件信息
/// </summary>
public sealed class FileInfoDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
