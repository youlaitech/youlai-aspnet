using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Config;

/// <summary>
/// 配置表单
/// </summary>
public sealed class ConfigForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("configName")]
    [Required(ErrorMessage = "配置名称不能为空")]
    public string? ConfigName { get; init; }

    [JsonPropertyName("configKey")]
    [Required(ErrorMessage = "配置键不能为空")]
    public string? ConfigKey { get; init; }

    [JsonPropertyName("configValue")]
    [Required(ErrorMessage = "配置值不能为空")]
    public string? ConfigValue { get; init; }

    [JsonPropertyName("remark")]
    public string? Remark { get; init; }
}
