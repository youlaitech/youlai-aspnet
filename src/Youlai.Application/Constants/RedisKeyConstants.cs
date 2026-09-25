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
    /// 表单模块键前缀
    /// </summary>
    public static class Form
    {
        /// <summary>已发布表单渲染规则（formKey 维度，发布/停用/改规则/删除时主动失效）</summary>
        public const string RenderRule = "form:render:{0}";

        /// <summary>公开表单匿名提交计数（formKey:ip 维度，固定窗口防刷）</summary>
        public const string PublicSubmitCount = "form:public:submit:{0}:{1}";
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
