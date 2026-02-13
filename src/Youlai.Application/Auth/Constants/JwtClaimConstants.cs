namespace Youlai.Application.Auth.Constants;

/// <summary>
/// JWT Claim 常量
/// </summary>
public static class JwtClaimConstants
{
    /// <summary>
    /// Token 类型
    /// </summary>
    public const string TokenType = "tokenType";

    /// <summary>
    /// 用户ID
    /// </summary>
    public const string UserId = "userId";

    /// <summary>
    /// 部门ID
    /// </summary>
    public const string DeptId = "deptId";

    /// <summary>
    /// 数据权限列表（支持多角色）
    /// </summary>
    public const string DataScopes = "dataScopes";

    /// <summary>
    /// 权限标识集合
    /// </summary>
    public const string Authorities = "authorities";

    /// <summary>
    /// Token 版本号
    /// </summary>
    public const string TokenVersion = "tokenVersion";
}
