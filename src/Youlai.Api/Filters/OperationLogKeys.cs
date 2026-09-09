namespace Youlai.Api.Filters;

/// <summary>
/// 操作日志在 HttpContext.Items 中传递操作人身份的键。
/// 用于登录/登出等认证前置场景：此时 JWT 尚未签发或已失效，HttpContext.User 无 claims，
/// 由业务侧把操作人塞入 Items，LogActionFilter 落库时读取，避免登录日志查不到操作人。
/// </summary>
public static class OperationLogKeys
{
    /// <summary>操作人ID</summary>
    public const string UserId = "__operation_log_user_id";

    /// <summary>操作人名称</summary>
    public const string UserName = "__operation_log_user_name";
}
