namespace Youlai.Domain.Entities;

/// <summary>
/// 本地用户与外部账号的关联
/// </summary>
public sealed class SysUserIdentity
{
    /// <summary>关联记录 ID</summary>
    public long Id { get; set; }

    /// <summary>本地用户 ID，角色与数据权限归属此用户</summary>
    public long UserId { get; set; }

    /// <summary>具体认证源标识，如 wechat-mini-main</summary>
    public string Provider { get; set; } = null!;

    /// <summary>来源方账号 ID，小程序即 OpenID</summary>
    public string ProviderAccountId { get; set; } = null!;

    /// <summary>外部账号展示名，不参与身份匹配</summary>
    public string? DisplayName { get; set; }

    /// <summary>外部账号头像，不覆盖本地资料</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>最近一次通过该外部账号登录的时间</summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>关联创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }
}
