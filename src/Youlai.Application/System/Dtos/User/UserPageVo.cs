using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 用户分页数据
/// </summary>
public sealed class UserPageVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; init; } = string.Empty;

    [JsonPropertyName("mobile")]
    public string? Mobile { get; init; }

    [JsonPropertyName("gender")]
    public int? Gender { get; init; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("deptName")]
    public string? DeptName { get; init; }

    [JsonPropertyName("roleNames")]
    public string? RoleNames { get; init; }

    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }
}
