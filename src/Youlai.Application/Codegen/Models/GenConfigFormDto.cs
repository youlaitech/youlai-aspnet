using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.Codegen.Models;

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
    [Required(ErrorMessage = "表名不能为空")]
    [StringLength(128, ErrorMessage = "表名不能超过 128 个字符")]
    [RegularExpression("^[A-Za-z_][A-Za-z0-9_]*$", ErrorMessage = "表名只能包含字母、数字和下划线")]
    [JsonPropertyName("tableName")]
    public string? TableName { get; init; }

    /// <summary>
    /// 业务名
    /// </summary>
    [StringLength(100, ErrorMessage = "业务名不能超过 100 个字符")]
    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    /// <summary>
    /// 模块名
    /// </summary>
    [Required(ErrorMessage = "模块名不能为空")]
    [StringLength(50, ErrorMessage = "模块名不能超过 50 个字符")]
    [RegularExpression("^[A-Za-z][A-Za-z0-9_]*$", ErrorMessage = "模块名只能包含字母、数字和下划线，且以字母开头")]
    [JsonPropertyName("moduleName")]
    public string? ModuleName { get; init; }

    /// <summary>
    /// 包名
    /// </summary>
    [RegularExpression("^[A-Za-z_][A-Za-z0-9_.]*$", ErrorMessage = "包名只能包含字母、数字、下划线和点")]
    [JsonPropertyName("packageName")]
    public string? PackageName { get; init; }

    /// <summary>
    /// 实体名
    /// </summary>
    [Required(ErrorMessage = "实体名不能为空")]
    [StringLength(100, ErrorMessage = "实体名不能超过 100 个字符")]
    [RegularExpression("^[A-Za-z][A-Za-z0-9_]*$", ErrorMessage = "实体名只能包含字母、数字和下划线，且以字母开头")]
    [JsonPropertyName("entityName")]
    public string? EntityName { get; init; }

    /// <summary>
    /// 作者
    /// </summary>
    [StringLength(50, ErrorMessage = "作者不能超过 50 个字符")]
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
    [RegularExpression("^(classic|crud)$", ErrorMessage = "页面类型只能是 classic 或 crud")]
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
