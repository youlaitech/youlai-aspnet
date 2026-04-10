using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Config;

/// <summary>
/// 配置表单
/// </summary>
public sealed class ConfigForm
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 配置名称
    /// </summary>
    [JsonPropertyName("configName")]
    [Required(ErrorMessage = "配置名称不能为空")]
    public string? ConfigName { get; init; }

    /// <summary>
    /// 配置键
    /// </summary>
    [JsonPropertyName("configKey")]
    [Required(ErrorMessage = "配置键不能为空")]
    public string? ConfigKey { get; init; }

    /// <summary>
    /// 配置值
    /// </summary>
    [JsonPropertyName("configValue")]
    [Required(ErrorMessage = "配置值不能为空")]
    public string? ConfigValue { get; init; }

    /// <summary>
    /// 备注
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; init; }
}
