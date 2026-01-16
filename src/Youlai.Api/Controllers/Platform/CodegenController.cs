using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.Codegen.Dtos;
using Youlai.Application.Platform.Codegen.Services;

namespace Youlai.Api.Controllers.Platform;

/// <summary>
/// 浠ｇ爜鐢熸垚鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵鏁版嵁琛ㄦ煡璇€侀厤缃淮鎶ゃ€侀瑙堜互鍙婁笅杞借兘鍔?
/// </remarks>
[ApiController]
[Route("api/v1/codegen")]
[Authorize]
public sealed class CodegenController : ControllerBase
{
    private readonly ICodegenService _codegenService;

    public CodegenController(ICodegenService codegenService)
    {
        _codegenService = codegenService;
    }

    /// <summary>
    /// 鏁版嵁琛ㄥ垎椤?
    /// </summary>
    [HttpGet("table")]
    [HasPerm("tool:codegen:list")]
    public Task<PageResult<CodegenTableDto>> GetTablePage([FromQuery] CodegenTableQuery query, CancellationToken cancellationToken)
    {
        return _codegenService.GetTablePageAsync(query, cancellationToken);
    }

    /// <summary>
    /// 鑾峰彇鐢熸垚閰嶇疆
    /// </summary>
    [HttpGet("{tableName}/config")]
    [HasPerm("tool:codegen:config")]
    public async Task<Result<GenConfigFormDto>> GetConfig([FromRoute] string tableName, CancellationToken cancellationToken)
    {
        var data = await _codegenService.GetConfigAsync(tableName, cancellationToken);
        return Result.Success(data);
    }

    /// <summary>
    /// 淇濆瓨鐢熸垚閰嶇疆
    /// </summary>
    [HttpPost("{tableName}/config")]
    [HasPerm("tool:codegen:save")]
    public async Task<Result<object?>> SaveConfig([FromRoute] string tableName, [FromBody] GenConfigFormDto formData, CancellationToken cancellationToken)
    {
        var ok = await _codegenService.SaveConfigAsync(tableName, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鍒犻櫎鐢熸垚閰嶇疆
    /// </summary>
    [HttpDelete("{tableName}/config")]
    [HasPerm("tool:codegen:delete")]
    public async Task<Result<object?>> DeleteConfig([FromRoute] string tableName, CancellationToken cancellationToken)
    {
        var ok = await _codegenService.DeleteConfigAsync(tableName, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 棰勮鐢熸垚浠ｇ爜
    /// </summary>
    [HttpGet("{tableName}/preview")]
    [HasPerm("tool:codegen:preview")]
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
    /// 涓嬭浇浠ｇ爜
    /// </summary>
    [HttpGet("{tableName}/download")]
    [HasPerm("tool:codegen:download")]
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
