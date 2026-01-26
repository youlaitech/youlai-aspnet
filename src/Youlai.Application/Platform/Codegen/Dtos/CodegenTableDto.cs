using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 代码生成表信息
/// </summary>
public sealed class CodegenTableDto
{
    /// <summary>
    /// 表名
    /// </summary>
    [JsonPropertyName("tableName")]
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// 表备注
    /// </summary>
    [JsonPropertyName("tableComment")]
    public string TableComment { get; init; } = string.Empty;

    /// <summary>
    /// 引擎
    /// </summary>
    [JsonPropertyName("engine")]
    public string Engine { get; init; } = string.Empty;

    /// <summary>
    /// 排序规则
    /// </summary>
    [JsonPropertyName("tableCollation")]
    public string TableCollation { get; init; } = string.Empty;

    /// <summary>
    /// 字符集
    /// </summary>
    [JsonPropertyName("charset")]
    public string Charset { get; init; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createTime")]
    public string CreateTime { get; init; } = string.Empty;

    /// <summary>
    /// 是否已配置
    /// </summary>
    [JsonPropertyName("isConfigured")]
    public int IsConfigured { get; init; }
}
