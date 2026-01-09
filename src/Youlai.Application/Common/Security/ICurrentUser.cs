using System.Security.Claims;

namespace Youlai.Application.Common.Security;

/// <summary>
/// 当前登录用户上下文
/// </summary>
public interface ICurrentUser
{
    ClaimsPrincipal? Principal { get; }

    long? UserId { get; }

    long? DeptId { get; }

    DataScope? DataScope { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsRoot { get; }
}
