using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 鐢ㄦ埛鍒嗛〉鏌ヨ鍙傛暟
/// </summary>
public sealed class UserQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public int? Status { get; init; }

    public long? DeptId { get; init; }

    public string? RoleIds { get; init; }

    public string? CreateTime { get; init; }

    public string? Field { get; init; }

    public string? Direction { get; init; }
}
