namespace Youlai.Domain.Entities;

/// <summary>
/// 用户第三方账号绑定
/// </summary>
public sealed class SysUserSocial
{
    /// <summary>
    /// 主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 平台类型
    /// </summary>
    public string Platform { get; set; } = null!;

    /// <summary>
    /// 平台openid
    /// </summary>
    public string OpenId { get; set; } = null!;

    /// <summary>
    /// 微信unionid
    /// </summary>
    public string? UnionId { get; set; }

    /// <summary>
    /// 第三方昵称
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 第三方头像URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 微信session_key
    /// </summary>
    public string? SessionKey { get; set; }

    /// <summary>
    /// 是否已验证
    /// </summary>
    public bool Verified { get; set; } = true;

    /// <summary>
    /// 绑定时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }
}
