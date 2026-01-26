using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Config;

/// <summary>
/// 配置分页数据
/// </summary>
public sealed class ConfigPageVo
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 配置名称
    /// </summary>
    [JsonPropertyName("configName")]
    public string ConfigName { get; init; } = string.Empty;

    /// <summary>
    /// 配置键
    /// </summary>
    [JsonPropertyName("configKey")]
    public string ConfigKey { get; init; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    [JsonPropertyName("configValue")]
    public string ConfigValue { get; init; } = string.Empty;

    /// <summary>
    /// 备注
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; init; }
}
