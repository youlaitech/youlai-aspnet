namespace Youlai.Infrastructure.Options;

/// <summary>
/// 安全配置
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public SessionOptions Session { get; init; } = new();

    /// <summary>
    /// 会话配置
    /// </summary>
    public sealed class SessionOptions
    {
        public int AccessTokenTimeToLive { get; init; } = 7200;

        public int RefreshTokenTimeToLive { get; init; } = 604800;

        public JwtOptions Jwt { get; init; } = new();

        /// <summary>
        /// JWT 配置
        /// </summary>
        public sealed class JwtOptions
        {
            public string SecretKey { get; init; } = string.Empty;
        }
    }
}
