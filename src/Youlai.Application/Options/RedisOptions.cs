namespace Youlai.Application.Options;

/// <summary>
/// Redis 连接配置项
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = string.Empty;
}
