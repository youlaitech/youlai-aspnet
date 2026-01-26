using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 代码生成配置表单
/// </summary>
public sealed class GenConfigFormDto
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 表名
    /// </summary>
    [JsonPropertyName("tableName")]
    public string? TableName { get; init; }

    /// <summary>
    /// 业务名
    /// </summary>
    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    /// <summary>
    /// 模块名
    /// </summary>
    [JsonPropertyName("moduleName")]
    public string? ModuleName { get; init; }

    /// <summary>
    /// 包名
    /// </summary>
    [JsonPropertyName("packageName")]
    public string? PackageName { get; init; }

    /// <summary>
    /// 实体名
    /// </summary>
    [JsonPropertyName("entityName")]
    public string? EntityName { get; init; }

    /// <summary>
    /// 作者
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; init; }

    /// <summary>
    /// 上级菜单ID
    /// </summary>
    [JsonPropertyName("parentMenuId")]
    public long? ParentMenuId { get; init; }

    /// <summary>
    /// 后端应用名
    /// </summary>
    [JsonPropertyName("backendAppName")]
    public string? BackendAppName { get; init; }

    /// <summary>
    /// 前端应用名
    /// </summary>
    [JsonPropertyName("frontendAppName")]
    public string? FrontendAppName { get; init; }

    /// <summary>
    /// 页面类型
    /// </summary>
    [JsonPropertyName("pageType")]
    public string? PageType { get; init; }

    /// <summary>
    /// 去除表前缀
    /// </summary>
    [JsonPropertyName("removeTablePrefix")]
    public string? RemoveTablePrefix { get; init; }

    /// <summary>
    /// 字段配置
    /// </summary>
    [JsonPropertyName("fieldConfigs")]
    public IReadOnlyCollection<FieldConfigDto>? FieldConfigs { get; init; }
}
