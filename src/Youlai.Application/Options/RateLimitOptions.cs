namespace Youlai.Application.Options;

/// <summary>
/// 限流配置（对应 appsettings.json 的 RateLimit 节点）
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public IpRateLimitOptions Ip { get; init; } = new();

    /// <summary>
    /// IP 限流配置项
    /// </summary>
    public sealed class IpRateLimitOptions
    {
        /// <summary>是否启用 IP 全局限流</summary>
        public bool Enabled { get; init; } = true;

        /// <summary>窗口内最大请求数</summary>
        public int Limit { get; init; } = 1000;

        /// <summary>滑动窗口大小</summary>
        public TimeSpan Window { get; init; } = TimeSpan.FromSeconds(60);
    }
}
