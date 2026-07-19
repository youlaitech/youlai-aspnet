namespace Youlai.Application.Options;

/// <summary>
/// 数据库配置项
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>数据库连接字符串</summary>
    public string ConnectionString { get; init; } = string.Empty;
}
