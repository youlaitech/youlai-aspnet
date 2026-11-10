namespace Youlai.Api.Filters;

/// <summary>
/// 操作日志操作人键：登录/登出时 JWT 尚未签发或已失效，由业务侧写入 HttpContext.Items，LogActionFilter 落库时读取
/// </summary>
public static class OperationLogKeys
{
    /// <summary>操作人ID</summary>
    public const string UserId = "__operation_log_user_id";

    /// <summary>操作人名称</summary>
    public const string UserName = "__operation_log_user_name";
}
