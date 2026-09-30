using System.Text.Json.Serialization;

namespace Youlai.Application.Codegen.Models;

/// <summary>
/// AI 推断的代码生成配置
/// </summary>
public sealed class GenConfigAiDto
{
    /// <summary>
    /// 业务名
    /// </summary>
    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    /// <summary>
    /// 字段配置列表
    /// </summary>
    [JsonPropertyName("fieldConfigs")]
    public IReadOnlyCollection<GenConfigAiFieldDto>? FieldConfigs { get; init; }
}

/// <summary>
/// AI 推断的字段配置
/// </summary>
public sealed class GenConfigAiFieldDto
{
    /// <summary>
    /// 列名，须与入参一致
    /// </summary>
    [JsonPropertyName("columnName")]
    public string? ColumnName { get; init; }

    /// <summary>
    /// 字段描述
    /// </summary>
    [JsonPropertyName("fieldComment")]
    public string? FieldComment { get; init; }

    /// <summary>
    /// 表单类型枚举名
    /// </summary>
    [JsonPropertyName("formType")]
    public string? FormType { get; init; }

    /// <summary>
    /// 查询类型枚举名
    /// </summary>
    [JsonPropertyName("queryType")]
    public string? QueryType { get; init; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [JsonPropertyName("isRequired")]
    public int? IsRequired { get; init; }

    /// <summary>
    /// 是否在列表显示
    /// </summary>
    [JsonPropertyName("isShowInList")]
    public int? IsShowInList { get; init; }

    /// <summary>
    /// 是否在表单显示
    /// </summary>
    [JsonPropertyName("isShowInForm")]
    public int? IsShowInForm { get; init; }

    /// <summary>
    /// 是否在查询条件显示
    /// </summary>
    [JsonPropertyName("isShowInQuery")]
    public int? IsShowInQuery { get; init; }

    /// <summary>
    /// 字典类型编码
    /// </summary>
    [JsonPropertyName("dictType")]
    public string? DictType { get; init; }
}
