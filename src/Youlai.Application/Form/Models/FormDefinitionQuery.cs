using Youlai.Application.Models;

namespace Youlai.Application.Form.Models;

/// <summary>
/// 表单定义分页查询参数
/// </summary>
public sealed class FormDefinitionQuery : BaseQuery
{
    /// <summary>关键字（表单名称/标识）</summary>
    public string? Keywords { get; init; }

    /// <summary>状态 0草稿 1已发布 -1已停用</summary>
    public int? Status { get; init; }

    /// <summary>表单类型 normal通用表单 workflow审批表单</summary>
    public string? Category { get; init; }
}
