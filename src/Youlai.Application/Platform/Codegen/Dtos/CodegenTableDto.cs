using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 代码生成表信息
/// </summary>
public sealed class CodegenTableDto
{
    [JsonPropertyName("tableName")]
    public string TableName { get; init; } = string.Empty;

    [JsonPropertyName("tableComment")]
    public string TableComment { get; init; } = string.Empty;

    [JsonPropertyName("engine")]
    public string Engine { get; init; } = string.Empty;

    [JsonPropertyName("tableCollation")]
    public string TableCollation { get; init; } = string.Empty;

    [JsonPropertyName("charset")]
    public string Charset { get; init; } = string.Empty;

    [JsonPropertyName("createTime")]
    public string CreateTime { get; init; } = string.Empty;

    [JsonPropertyName("isConfigured")]
    public int IsConfigured { get; init; }
}
