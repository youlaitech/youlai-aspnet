using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Role;

/// <summary>
/// 瑙掕壊鍒嗛〉鏁版嵁
/// </summary>
public sealed class RolePageVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }

    [JsonPropertyName("updateTime")]
    public string? UpdateTime { get; init; }
}
