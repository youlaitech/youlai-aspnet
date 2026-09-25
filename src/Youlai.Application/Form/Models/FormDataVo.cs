using System.Text.Json.Serialization;

namespace Youlai.Application.Form.Models;

/// <summary>
/// 表单数据对象（分页行与详情回显共用）
/// </summary>
public sealed class FormDataVo
{
    /// <summary>数据ID</summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>表单版本（提交时快照）</summary>
    [JsonPropertyName("formVersion")]
    public int FormVersion { get; init; }

    /// <summary>表单数据（field -> value 映射的 JSON 字符串）</summary>
    [JsonPropertyName("dataJson")]
    public string DataJson { get; init; } = "{}";

    /// <summary>回显规则（提交时版本的快照，仅详情接口填充）</summary>
    [JsonPropertyName("formJson")]
    public string? FormJson { get; set; }

    /// <summary>回显全局配置（提交时版本的快照，仅详情接口填充）</summary>
    [JsonPropertyName("optionsJson")]
    public string? OptionsJson { get; set; }

    /// <summary>提交人ID（匿名提交为空）</summary>
    [JsonPropertyName("createBy")]
    public long? CreateBy { get; init; }

    /// <summary>提交人昵称（匿名提交为空）</summary>
    [JsonPropertyName("createByName")]
    public string? CreateByName { get; init; }

    /// <summary>提交时间</summary>
    [JsonPropertyName("createTime")]
    public DateTime? CreateTime { get; init; }
}
