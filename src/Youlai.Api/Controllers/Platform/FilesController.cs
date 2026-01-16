using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.File.Dtos;
using Youlai.Application.Platform.File.Services;

namespace Youlai.Api.Controllers.Platform;

/// <summary>
/// 鏂囦欢鎺ュ彛
/// </summary>
[ApiController]
[Route("api/v1/files")]
[Authorize]
public sealed class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// 鏂囦欢涓婁紶
    /// </summary>
    /// <param name="file">琛ㄥ崟鏂囦欢瀛楁锛屽瓧娈靛悕鍥哄畾涓?file</param>
    /// <param name="cancellationToken">鍙栨秷浠ょ墝</param>
    [HttpPost]
    public async Task<Result<FileInfoDto>> UploadFile([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessException(ResultCode.RequestRequiredParameterIsEmpty, "鏂囦欢涓嶈兘涓虹┖");
        }

        await using var stream = file.OpenReadStream();
        var result = await _fileService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);
        return Result.Success(result);
    }

    /// <summary>
    /// 鏂囦欢鍒犻櫎
    /// </summary>
    /// <param name="filePath">鏂囦欢璺緞鎴栧畬鏁?URL</param>
    /// <param name="cancellationToken">鍙栨秷浠ょ墝</param>
    [HttpDelete]
    public async Task<Result<object?>> DeleteFile([FromQuery] string filePath, CancellationToken cancellationToken)
    {
        var ok = await _fileService.DeleteAsync(filePath, cancellationToken);
        return Result.Judge(ok);
    }
}
