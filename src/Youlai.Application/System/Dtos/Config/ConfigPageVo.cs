using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Config;

/// <summary>
/// 閰嶇疆鍒嗛〉鏁版嵁
/// </summary>
public sealed class ConfigPageVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("configName")]
    public string ConfigName { get; init; } = string.Empty;

    [JsonPropertyName("configKey")]
    public string ConfigKey { get; init; } = string.Empty;

    [JsonPropertyName("configValue")]
    public string ConfigValue { get; init; } = string.Empty;
}
