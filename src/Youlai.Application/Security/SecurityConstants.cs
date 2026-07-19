namespace Youlai.Application.Security;

/// <summary>
/// 安全相关常量
/// </summary>
public static class SecurityConstants
{
    /// <summary>角色声明前缀，拼接角色名组成 Claim</summary>
    public const string RolePrefix = "ROLE_";

    /// <summary>超级管理员角色代码</summary>
    public const string RootRoleCode = "ROOT";

    /// <summary>权限策略前缀，拼接权限标识组成策略名</summary>
    public const string PermissionPolicyPrefix = "Perm:";
}
