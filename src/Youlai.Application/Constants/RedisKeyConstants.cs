namespace Youlai.Application.Constants;

/// <summary>
/// Redis 缓存键名常量
/// </summary>
internal static class RedisKeyConstants
{
    /// <summary>
    /// 系统模块键前缀
    /// </summary>
    internal static class System
    {
        internal const string RolePerms = "system:role:perms";

        internal const string Config = "system:config";
    }

    /// <summary>
    /// 认证模块键前缀（黑名单、Token 版本等）
    /// </summary>
    internal static class Auth
    {
        internal const string BlacklistToken = "auth:token:blacklist:{0}";
        internal const string UserTokenVersion = "auth:user:token_version:{0}";
        internal const string QrCode = "auth:qr_code:{0}";
    }

    /// <summary>
    /// 验证码键前缀
    /// </summary>
    internal static class Captcha
    {
        internal const string ImageCode = "captcha:image:{0}";

        internal const string MobileCode = "captcha:mobile:{0}";

        internal const string EmailCode = "captcha:email:{0}";
    }

    /// <summary>
    /// 微信相关键前缀
    /// </summary>
    internal static class Wechat
    {
        internal const string AccessToken = "wechat:access_token:{0}";
    }
}
