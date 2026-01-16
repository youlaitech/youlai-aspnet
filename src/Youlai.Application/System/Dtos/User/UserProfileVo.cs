using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 涓汉璧勬枡鏁版嵁
/// </summary>
public sealed class UserProfileVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    [JsonPropertyName("gender")]
    public int? Gender { get; init; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("deptName")]
    public string? DeptName { get; init; }

    [JsonPropertyName("roleNames")]
    public string? RoleNames { get; init; }

    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }
}
