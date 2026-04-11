using Youlai.Application.Exceptions;
using Youlai.Application.Results;
using Youlai.Application.Security;

namespace Youlai.Application.Extensions;

/// <summary>
/// CurrentUser 扩展方法
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// 获取当前用户ID，无效时抛出 BusinessException
    /// </summary>
    public static long GetRequiredUserId(this ICurrentUser currentUser)
    {
        var userId = currentUser.UserId;
        if (!userId.HasValue || userId.Value <= 0)
        {
            throw new BusinessException(ResultCode.AccessTokenInvalid);
        }

        return userId.Value;
    }
}
