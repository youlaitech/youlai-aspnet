namespace Youlai.Domain.Entities;

/// <summary>
/// 用户第三方账号绑定
/// </summary>
public sealed class SysUserSocial
{
    public long Id { get; set; }

    public long UserId { get; set; }

    /// <summary>
    /// 平台类型（wechat_miniapp=微信小程序）
    /// </summary>
    public string Platform { get; set; } = null!;

    public string OpenId { get; set; } = null!;

    public string? UnionId { get; set; }

    public string? Nickname { get; set; }

    public string? Avatar { get; set; }

    /// <summary>
    /// 微信session_key
    /// </summary>
    public string? SessionKey { get; set; }

    public bool Verified { get; set; } = true;

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }
}
