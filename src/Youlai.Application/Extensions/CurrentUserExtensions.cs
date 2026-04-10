using Youlai.Application.Exceptions;
using Youlai.Application.Results;
using Youlai.Application.Security;

namespace Youlai.Application.Extensions;

/// <summary>
/// CurrentUser extension methods
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// Get required current user ID, throws BusinessException if invalid
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
