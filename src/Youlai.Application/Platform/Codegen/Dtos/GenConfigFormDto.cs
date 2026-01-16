using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Codegen.Dtos;

/// <summary>
/// 代码生成配置表单
/// </summary>
public sealed class GenConfigFormDto
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("tableName")]
    public string? TableName { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("moduleName")]
    public string? ModuleName { get; init; }

    [JsonPropertyName("packageName")]
    public string? PackageName { get; init; }

    [JsonPropertyName("entityName")]
    public string? EntityName { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("parentMenuId")]
    public long? ParentMenuId { get; init; }

    [JsonPropertyName("backendAppName")]
    public string? BackendAppName { get; init; }

    [JsonPropertyName("frontendAppName")]
    public string? FrontendAppName { get; init; }

    [JsonPropertyName("pageType")]
    public string? PageType { get; init; }

    [JsonPropertyName("removeTablePrefix")]
    public string? RemoveTablePrefix { get; init; }

    [JsonPropertyName("fieldConfigs")]
    public IReadOnlyCollection<FieldConfigDto>? FieldConfigs { get; init; }
}
