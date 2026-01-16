using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Config;

/// <summary>
/// 閰嶇疆琛ㄥ崟
/// </summary>
public sealed class ConfigForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("configName")]
    [Required(ErrorMessage = "閰嶇疆鍚嶇О涓嶈兘涓虹┖")]
    public string? ConfigName { get; init; }

    [JsonPropertyName("configKey")]
    [Required(ErrorMessage = "閰嶇疆閿笉鑳戒负绌?)]
    public string? ConfigKey { get; init; }

    [JsonPropertyName("configValue")]
    [Required(ErrorMessage = "閰嶇疆鍊间笉鑳戒负绌?)]
    public string? ConfigValue { get; init; }

    [JsonPropertyName("remark")]
    public string? Remark { get; init; }
}
