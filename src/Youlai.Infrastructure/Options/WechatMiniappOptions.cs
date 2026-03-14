namespace Youlai.Infrastructure.Options;

/// <summary>
/// 微信小程序配置
/// </summary>
public sealed class WechatMiniappOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "WechatMiniapp";

    /// <summary>
    /// 小程序AppId
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 小程序AppSecret
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// 新用户默认角色ID（默认为访客角色ID=3）
    /// </summary>
    public long DefaultRoleId { get; set; } = 3;
}
