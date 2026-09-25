using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Attributes;
using Youlai.Application.Form;
using Youlai.Application.Form.Models;
using Youlai.Application.Models;
using Youlai.Application.Results;
using Youlai.Domain.Enums;

namespace Youlai.Api.Controllers.Form;

/// <summary>
/// 表单控制层（表单定义、访问菜单、表单数据）
/// </summary>
[ApiController]
[Route("api/v1/forms")]
[Authorize]
[Tags("12.表单接口")]
public sealed class FormsController : ControllerBase
{
    private readonly IFormDefinitionService _formDefinitionService;
    private readonly IFormDataService _formDataService;

    public FormsController(IFormDefinitionService formDefinitionService, IFormDataService formDataService)
    {
        _formDefinitionService = formDefinitionService;
        _formDataService = formDataService;
    }

    /// <summary>
    /// 表单定义分页列表
    /// </summary>
    [HttpGet]
    [Log(LogModule.FORM, ActionType.LIST)]
    public async Task<PageResult<FormDefinitionPageVo>> GetFormDefinitionPage([FromQuery] FormDefinitionQuery queryParams, CancellationToken cancellationToken)
    {
        return await _formDefinitionService.GetFormDefinitionPageAsync(queryParams, cancellationToken);
    }

    /// <summary>
    /// 获取审批表单下拉选项（工作流设计器绑定表单用）
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Option<string>>>> ListWorkflowFormOptions(CancellationToken cancellationToken)
    {
        var options = await _formDefinitionService.ListWorkflowFormOptionsAsync(cancellationToken);
        return Result.Success(options);
    }

    /// <summary>
    /// 获取表单定义表单数据
    /// </summary>
    [HttpGet("{formId:long}/form")]
    public async Task<Result<FormDefinitionForm>> GetFormDefinitionForm([FromRoute] long formId, CancellationToken cancellationToken)
    {
        var formData = await _formDefinitionService.GetFormDefinitionFormAsync(formId, cancellationToken);
        return Result.Success(formData);
    }

    /// <summary>
    /// 新增表单定义
    /// </summary>
    [HttpPost]
    [HasPerm("form:definition:create")]
    [Log(LogModule.FORM, ActionType.INSERT)]
    public async Task<Result<object?>> SaveFormDefinition([FromBody] FormDefinitionForm formData, CancellationToken cancellationToken)
    {
        var ok = await _formDefinitionService.SaveFormDefinitionAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 修改表单定义
    /// </summary>
    [HttpPut("{formId:long}")]
    [HasPerm("form:definition:update")]
    [Log(LogModule.FORM, ActionType.UPDATE)]
    public async Task<Result<object?>> UpdateFormDefinition([FromRoute] long formId, [FromBody] FormDefinitionForm formData, CancellationToken cancellationToken)
    {
        var ok = await _formDefinitionService.UpdateFormDefinitionAsync(formId, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 删除表单定义
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("form:definition:delete")]
    [Log(LogModule.FORM, ActionType.DELETE)]
    public async Task<Result<object?>> RemoveFormDefinition([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _formDefinitionService.RemoveFormDefinitionAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 发布表单
    /// </summary>
    [HttpPut("{formId:long}/publish")]
    [HasPerm("form:definition:update")]
    [Log(LogModule.FORM, ActionType.UPDATE)]
    public async Task<Result<object?>> PublishFormDefinition([FromRoute] long formId, CancellationToken cancellationToken)
    {
        var ok = await _formDefinitionService.PublishFormDefinitionAsync(formId, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 停用表单
    /// </summary>
    [HttpPut("{formId:long}/disable")]
    [HasPerm("form:definition:update")]
    [Log(LogModule.FORM, ActionType.UPDATE)]
    public async Task<Result<object?>> DisableFormDefinition([FromRoute] long formId, CancellationToken cancellationToken)
    {
        var ok = await _formDefinitionService.DisableFormDefinitionAsync(formId, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 获取表单渲染规则
    /// </summary>
    [HttpGet("{formKey}/render")]
    public async Task<Result<FormRenderVo>> GetFormRender([FromRoute] string formKey, CancellationToken cancellationToken)
    {
        var renderVo = await _formDefinitionService.GetFormRenderAsync(formKey, cancellationToken);
        return Result.Success(renderVo);
    }

    /// <summary>
    /// 获取表单菜单配置（向导回显）
    /// </summary>
    [HttpGet("{formId:long}/menu")]
    public async Task<Result<FormMenuVo?>> GetFormMenu([FromRoute] long formId, CancellationToken cancellationToken)
    {
        var menuVo = await _formDefinitionService.GetFormMenuAsync(formId, cancellationToken);
        return Result.Success(menuVo);
    }

    /// <summary>
    /// 生成表单访问菜单
    /// </summary>
    [HttpPost("{formId:long}/menu")]
    [HasPerm("form:definition:update")]
    [Log(LogModule.FORM, ActionType.INSERT)]
    public async Task<Result<object?>> SaveFormMenu([FromRoute] long formId, [FromBody] FormMenuForm menuForm, CancellationToken cancellationToken)
    {
        var ok = await _formDefinitionService.SaveFormMenuAsync(formId, menuForm, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 获取公开表单渲染规则（匿名）
    /// </summary>
    [HttpGet("public/{formKey}/render")]
    [AllowAnonymous]
    public async Task<Result<FormRenderVo>> GetPublicFormRender([FromRoute] string formKey, CancellationToken cancellationToken)
    {
        var renderVo = await _formDefinitionService.GetPublicFormRenderAsync(formKey, cancellationToken);
        return Result.Success(renderVo);
    }

    /// <summary>
    /// 匿名提交公开表单数据
    /// </summary>
    [HttpPost("public/{formKey}/data")]
    [AllowAnonymous]
    public async Task<Result<object?>> SavePublicFormData([FromRoute] string formKey, [FromBody] JsonElement data, CancellationToken cancellationToken)
    {
        var ok = await _formDataService.SavePublicFormDataAsync(formKey, data, GetClientIp(), cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 表单数据分页列表
    /// </summary>
    [HttpGet("{formKey}/data")]
    [HasPerm("form:data:list")]
    [Log(LogModule.FORM, ActionType.LIST)]
    public async Task<PageResult<FormDataVo>> GetFormDataPage([FromRoute] string formKey, [FromQuery] FormDataQuery queryParams, CancellationToken cancellationToken)
    {
        return await _formDataService.GetFormDataPageAsync(formKey, queryParams, cancellationToken);
    }

    /// <summary>
    /// 提交表单数据
    /// </summary>
    [HttpPost("{formKey}/data")]
    [Log(LogModule.FORM, ActionType.INSERT)]
    public async Task<Result<object?>> SaveFormData([FromRoute] string formKey, [FromBody] JsonElement data, CancellationToken cancellationToken)
    {
        var ok = await _formDataService.SaveFormDataAsync(formKey, data, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 获取表单数据详情（只读回显）
    /// </summary>
    [HttpGet("{formKey}/data/{dataId:long}")]
    [HasPerm("form:data:list")]
    public async Task<Result<FormDataVo>> GetFormDataDetail([FromRoute] string formKey, [FromRoute] long dataId, CancellationToken cancellationToken)
    {
        var dataVo = await _formDataService.GetFormDataDetailAsync(dataId, cancellationToken);
        if (dataVo is null)
        {
            throw new Youlai.Application.Exceptions.BusinessException(ResultCode.InvalidUserInput, "表单数据不存在");
        }

        return Result.Success(dataVo);
    }

    /// <summary>
    /// 删除表单数据
    /// </summary>
    [HttpDelete("{formKey}/data/{ids}")]
    [HasPerm("form:data:delete")]
    [Log(LogModule.FORM, ActionType.DELETE)]
    public async Task<Result<object?>> RemoveFormData([FromRoute] string formKey, [FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _formDataService.RemoveFormDataAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// AI 生成表单规则
    /// </summary>
    [HttpPost("ai-generate")]
    public async Task<Result<string>> GenerateRule([FromBody] FormAiGenerateForm form, CancellationToken cancellationToken)
    {
        var rule = await _formDefinitionService.AiGenerateRuleAsync(form.Description, cancellationToken);
        return Result.Success(rule);
    }

    /// <summary>
    /// 取客户端真实 IP：优先 X-Forwarded-For 首段，否则取远端地址
    /// </summary>
    private string GetClientIp()
    {
        var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
