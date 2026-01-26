using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 字段配置
/// </summary>
public sealed class FieldConfigDto
{
    /// <summary>
    /// 字段配置ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 列名
    /// </summary>
    [JsonPropertyName("columnName")]
    public string? ColumnName { get; init; }

    /// <summary>
    /// 列类型
    /// </summary>
    [JsonPropertyName("columnType")]
    public string? ColumnType { get; init; }

    /// <summary>
    /// 字段名
    /// </summary>
    [JsonPropertyName("fieldName")]
    public string? FieldName { get; init; }

    /// <summary>
    /// 字段类型
    /// </summary>
    [JsonPropertyName("fieldType")]
    public string? FieldType { get; init; }

    /// <summary>
    /// 字段备注
    /// </summary>
    [JsonPropertyName("fieldComment")]
    public string? FieldComment { get; init; }

    /// <summary>
    /// 列表是否展示
    /// </summary>
    [JsonPropertyName("isShowInList")]
    public int? IsShowInList { get; init; }

    /// <summary>
    /// 表单是否展示
    /// </summary>
    [JsonPropertyName("isShowInForm")]
    public int? IsShowInForm { get; init; }

    /// <summary>
    /// 查询是否展示
    /// </summary>
    [JsonPropertyName("isShowInQuery")]
    public int? IsShowInQuery { get; init; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [JsonPropertyName("isRequired")]
    public int? IsRequired { get; init; }

    /// <summary>
    /// 表单类型
    /// </summary>
    [JsonPropertyName("formType")]
    public int? FormType { get; init; }

    /// <summary>
    /// 查询类型
    /// </summary>
    [JsonPropertyName("queryType")]
    public int? QueryType { get; init; }

    /// <summary>
    /// 最大长度
    /// </summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    /// <summary>
    /// 字段排序
    /// </summary>
    [JsonPropertyName("fieldSort")]
    public int? FieldSort { get; init; }

    /// <summary>
    /// 字典类型
    /// </summary>
    [JsonPropertyName("dictType")]
    public string? DictType { get; init; }
}
