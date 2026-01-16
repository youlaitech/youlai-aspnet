using System;
using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Role;

/// <summary>
/// 瑙掕壊鍒嗛〉鏌ヨ鍙傛暟
/// </summary>
/// <remarks>
/// 鐢ㄤ簬瑙掕壊鍒嗛〉鎺ュ彛鐨勬煡璇㈡潯浠?
/// </remarks>
public sealed class RoleQuery : BaseQuery
{
    public string? Keywords { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }
}
