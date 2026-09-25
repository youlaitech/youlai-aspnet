using Youlai.Application.Form.Models;
using Youlai.Application.Models;
using Youlai.Application.Results;
using Youlai.Domain.Entities;

namespace Youlai.Application.Form;

/// <summary>
/// 表单定义业务接口
/// </summary>
public interface IFormDefinitionService
{
    /// <summary>表单定义分页列表</summary>
    Task<PageResult<FormDefinitionPageVo>> GetFormDefinitionPageAsync(FormDefinitionQuery queryParams, CancellationToken cancellationToken = default);

    /// <summary>表单定义表单数据（编辑回显）</summary>
    Task<FormDefinitionForm> GetFormDefinitionFormAsync(long formId, CancellationToken cancellationToken = default);

    /// <summary>新增表单定义（初始为草稿态）</summary>
    Task<bool> SaveFormDefinitionAsync(FormDefinitionForm formData, CancellationToken cancellationToken = default);

    /// <summary>修改表单定义（formKey 创建后不可修改；草稿态可改元数据）</summary>
    Task<bool> UpdateFormDefinitionAsync(long formId, FormDefinitionForm formData, CancellationToken cancellationToken = default);

    /// <summary>批量删除表单定义（级联清理访问菜单、提交数据与版本快照）</summary>
    Task<bool> RemoveFormDefinitionAsync(string ids, CancellationToken cancellationToken = default);

    /// <summary>发布表单（版本号+1，固化版本快照，刷新渲染缓存）</summary>
    Task<bool> PublishFormDefinitionAsync(long formId, CancellationToken cancellationToken = default);

    /// <summary>停用表单（清渲染缓存，已发出去的入口立即失效）</summary>
    Task<bool> DisableFormDefinitionAsync(long formId, CancellationToken cancellationToken = default);

    /// <summary>按 formKey 获取已发布表单的渲染规则（Redis 缓存）</summary>
    Task<FormRenderVo> GetFormRenderAsync(string formKey, CancellationToken cancellationToken = default);

    /// <summary>按 formKey 获取公开表单的渲染规则（匿名接口，仅 is_public=1 且已发布）</summary>
    Task<FormRenderVo> GetPublicFormRenderAsync(string formKey, CancellationToken cancellationToken = default);

    /// <summary>表单已生成的菜单配置（发布向导回显，未生成返回 null）</summary>
    Task<FormMenuVo?> GetFormMenuAsync(long formId, CancellationToken cancellationToken = default);

    /// <summary>生成表单访问菜单并授权可见角色（幂等：已生成时更新原菜单）</summary>
    Task<bool> SaveFormMenuAsync(long formId, FormMenuForm menuForm, CancellationToken cancellationToken = default);

    /// <summary>审批表单下拉选项（仅已发布且类型为 workflow 的表单）</summary>
    Task<IReadOnlyCollection<Option<string>>> ListWorkflowFormOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>按 formKey 查询表单定义（任意状态）</summary>
    Task<FormDefinition?> GetByFormKeyAsync(string formKey, CancellationToken cancellationToken = default);

    /// <summary>用 AI 把需求描述转成 form-create 规则，产出经结构校验后才返回</summary>
    Task<string> AiGenerateRuleAsync(string description, CancellationToken cancellationToken = default);
}
