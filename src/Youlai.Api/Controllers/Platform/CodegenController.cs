using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.Codegen.Dtos;
using Youlai.Application.Platform.Codegen.Services;

namespace Youlai.Api.Controllers.Platform;

/// <summary>
/// 代码生成接口
/// </summary>
/// <remarks>
/// 提供数据表查询、配置维护、预览以及下载能力。
/// </remarks>
[ApiController]
[Route("api/v1/codegen")]
[Authorize]
[Tags("13.代码生成")]
public sealed class CodegenController : ControllerBase
{
    private readonly ICodegenService _codegenService;

    public CodegenController(ICodegenService codegenService)
    {
        _codegenService = codegenService;
    }

    /// <summary>
    /// 数据表分页
    /// </summary>
    [HttpGet("table")]
    public Task<PageResult<CodegenTableDto>> GetTablePage([FromQuery] CodegenTableQuery query, CancellationToken cancellationToken)
    {
        return _codegenService.GetTablePageAsync(query, cancellationToken);
    }

    /// <summary>
    /// 获取生成配置
    /// </summary>
    [HttpGet("{tableName}/config")]
    public async Task<Result<GenConfigFormDto>> GetConfig([FromRoute] string tableName, CancellationToken cancellationToken)
    {
        var data = await _codegenService.GetConfigAsync(tableName, cancellationToken);
        return Result.Success(data);
    }

    /// <summary>
    /// 保存生成配置
    /// </summary>
    [HttpPost("{tableName}/config")]
    public async Task<Result<object?>> SaveConfig([FromRoute] string tableName, [FromBody] GenConfigFormDto formData, CancellationToken cancellationToken)
    {
        var ok = await _codegenService.SaveConfigAsync(tableName, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 删除生成配置
    /// </summary>
    [HttpDelete("{tableName}/config")]
    public async Task<Result<object?>> DeleteConfig([FromRoute] string tableName, CancellationToken cancellationToken)
    {
        var ok = await _codegenService.DeleteConfigAsync(tableName, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 预览生成代码
    /// </summary>
    [HttpGet("{tableName}/preview")]
    public async Task<Result<IReadOnlyCollection<CodegenPreviewDto>>> Preview(
        [FromRoute] string tableName,
        [FromQuery] string pageType = "classic",
        [FromQuery] string type = "ts",
        CancellationToken cancellationToken = default)
    {
        var list = await _codegenService.GetPreviewAsync(tableName, pageType, type, cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 下载代码
    /// </summary>
    [HttpGet("{tableName}/download")]
    public async Task<IActionResult> Download(
        [FromRoute] string tableName,
        [FromQuery] string pageType = "classic",
        [FromQuery] string type = "ts",
        CancellationToken cancellationToken = default)
    {
        var names = tableName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var (fileName, content) = await _codegenService.DownloadAsync(names, pageType, type, cancellationToken);
        return File(content, "application/zip", fileName);
    }
}
