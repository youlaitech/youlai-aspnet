using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 瀛楀吀鍒嗛〉鏁版嵁
/// </summary>
public sealed class DictPageVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("dictCode")]
    public string DictCode { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; init; }
}
