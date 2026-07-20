namespace Youlai.Application.Constants;

/// <summary>
/// Redis 缓存键名常量
/// </summary>
public static class RedisKeyConstants
{
    /// <summary>
    /// 系统模块键前缀
    /// </summary>
    public static class System
    {
        public const string RolePerms = "system:role:perms:{0}";

        public const string Config = "system:config";
    }

    /// <summary>
    /// 认证模块键前缀（黑名单、Token 版本等）
    /// </summary>
    public static class Auth
    {
        public const string BlacklistToken = "auth:token:blacklist:{0}";

        public const string UserTokenVersion = "auth:user:token_version:{0}";

        public const string QrCode = "auth:qr_code:{0}";
    }

    /// <summary>
    /// 验证码键前缀
    /// </summary>
    public static class Captcha
    {
        public const string ImageCode = "captcha:image:{0}";

        public const string MobileCode = "captcha:mobile:{0}";

        public const string EmailCode = "captcha:email:{0}";
    }

    /// <summary>
    /// 微信相关键前缀
    /// </summary>
    public static class Wechat
    {
        public const string AccessToken = "wechat:access_token:{0}";
    }
}
