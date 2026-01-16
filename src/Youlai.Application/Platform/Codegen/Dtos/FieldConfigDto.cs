using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 字段配置
/// </summary>
public sealed class FieldConfigDto
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("columnName")]
    public string? ColumnName { get; init; }

    [JsonPropertyName("columnType")]
    public string? ColumnType { get; init; }

    [JsonPropertyName("fieldName")]
    public string? FieldName { get; init; }

    [JsonPropertyName("fieldType")]
    public string? FieldType { get; init; }

    [JsonPropertyName("fieldComment")]
    public string? FieldComment { get; init; }

    [JsonPropertyName("isShowInList")]
    public int? IsShowInList { get; init; }

    [JsonPropertyName("isShowInForm")]
    public int? IsShowInForm { get; init; }

    [JsonPropertyName("isShowInQuery")]
    public int? IsShowInQuery { get; init; }

    [JsonPropertyName("isRequired")]
    public int? IsRequired { get; init; }

    [JsonPropertyName("formType")]
    public int? FormType { get; init; }

    [JsonPropertyName("queryType")]
    public int? QueryType { get; init; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    [JsonPropertyName("fieldSort")]
    public int? FieldSort { get; init; }

    [JsonPropertyName("dictType")]
    public string? DictType { get; init; }
}
