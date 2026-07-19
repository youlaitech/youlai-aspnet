namespace Youlai.Application.Options;

/// <summary>
/// 安全配置
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>会话相关配置</summary>
    public SessionOptions Session { get; init; } = new();

    /// <summary>
    /// 会话配置
    /// </summary>
    public sealed class SessionOptions
    {
        /// <summary>访问令牌有效期（秒），默认 7200</summary>
        public int AccessTokenTimeToLive { get; init; } = 7200;

        /// <summary>刷新令牌有效期（秒），默认 604800</summary>
        public int RefreshTokenTimeToLive { get; init; } = 604800;

        /// <summary>JWT 相关配置</summary>
        public JwtOptions Jwt { get; init; } = new();

        /// <summary>
        /// JWT 配置
        /// </summary>
        public sealed class JwtOptions
        {
            /// <summary>JWT 签名密钥</summary>
            public string SecretKey { get; init; } = string.Empty;
        }
    }
}
