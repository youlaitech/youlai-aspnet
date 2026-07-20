namespace Youlai.Domain.Entities;

/// <summary>
/// 用户第三方账号绑定
/// </summary>
public sealed class SysUserSocial
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>绑定用户 ID</summary>
    public long UserId { get; set; }

    /// <summary>
    /// 平台类型（wechat_miniapp=微信小程序）
    /// </summary>
    public string Platform { get; set; } = null!;

    /// <summary>平台内的唯一 OpenID</summary>
    public string OpenId { get; set; } = null!;

    /// <summary>跨应用唯一标识</summary>
    public string? UnionId { get; set; }

    /// <summary>平台昵称</summary>
    public string? Nickname { get; set; }

    /// <summary>平台头像</summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 微信session_key
    /// </summary>
    public string? SessionKey { get; set; }

    /// <summary>是否已实名校验</summary>
    public bool Verified { get; set; } = true;

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }
}
