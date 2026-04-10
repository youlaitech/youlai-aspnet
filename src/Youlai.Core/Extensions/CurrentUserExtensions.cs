using Youlai.Core.Exceptions;
using Youlai.Core.Results;
using Youlai.Core.Security;

namespace Youlai.Core.Extensions;

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
