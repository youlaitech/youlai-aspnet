namespace Youlai.Infrastructure.Constants;

internal static class RedisKeyConstants
{
    internal static class System
    {
        internal const string RolePerms = "system:role:perms";

        internal const string Config = "system:config";
    }

    internal static class Auth
    {
        internal const string BlacklistToken = "auth:token:blacklist:{0}";
        internal const string UserSecurityVersion = "auth:user:security_version:{0}";
    }

    internal static class Captcha
    {
        internal const string ImageCode = "captcha:image:{0}";

        internal const string MobileCode = "captcha:mobile:{0}";

        internal const string EmailCode = "captcha:email:{0}";
    }
}
