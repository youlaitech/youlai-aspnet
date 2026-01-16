using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Role;

/// <summary>
/// 瑙掕壊琛ㄥ崟
/// </summary>
public sealed class RoleForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("name")]
    [Required(ErrorMessage = "瑙掕壊鍚嶇О涓嶈兘涓虹┖")]
    public string? Name { get; init; }

    [JsonPropertyName("code")]
    [Required(ErrorMessage = "瑙掕壊缂栫爜涓嶈兘涓虹┖")]
    public string? Code { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    [JsonPropertyName("status")]
    [Range(0, 1, ErrorMessage = "瑙掕壊鐘舵€佷笉姝ｇ‘")]
    public int? Status { get; init; }

    [JsonPropertyName("dataScope")]
    public int? DataScope { get; init; }

    [JsonPropertyName("remark")]
    public string? Remark { get; init; }
}
