using System.Text.Json;
using Youlai.Application.Form.Models;
using Youlai.Application.Results;

namespace Youlai.Application.Form;

/// <summary>
/// 表单数据业务接口
/// </summary>
public interface IFormDataService
{
    /// <summary>提交表单数据（仅已发布表单可提交，提交时快照表单版本）</summary>
    Task<bool> SaveFormDataAsync(string formKey, JsonElement data, CancellationToken cancellationToken = default);

    /// <summary>匿名提交公开表单数据（formKey + IP 维度限流）</summary>
    Task<bool> SavePublicFormDataAsync(string formKey, JsonElement data, string ip, CancellationToken cancellationToken = default);

    /// <summary>表单数据分页列表</summary>
    Task<PageResult<FormDataVo>> GetFormDataPageAsync(string formKey, FormDataQuery queryParams, CancellationToken cancellationToken = default);

    /// <summary>表单数据详情（只读回显，规则取提交时版本的快照）</summary>
    Task<FormDataVo?> GetFormDataDetailAsync(long dataId, CancellationToken cancellationToken = default);

    /// <summary>批量删除表单数据</summary>
    Task<bool> RemoveFormDataAsync(string ids, CancellationToken cancellationToken = default);
}
