using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Youlai.Core.Exceptions;
using Youlai.Core.Results;
using Youlai.Core.Platform.File.Dtos;
using Youlai.Core.Platform.File.Services;

namespace Youlai.Api.Controllers.File;

/// <summary>
/// 文件接口
/// </summary>
[ApiController]
[Route("api/v1/files")]
[Authorize]
[Tags("10.文件接口")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// 文件上传
    /// </summary>
    /// <param name="request">上传表单数据（字段名固定为 file）</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<Result<FileInfoDto>> UploadFile([FromForm] FileUploadRequest request, CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
        {
            throw new BusinessException(ResultCode.RequestRequiredParameterIsEmpty, "文件不能为空");
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
    /// 文件删除
    /// </summary>
    /// <param name="filePath">文件路径或完整 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpDelete]
    public async Task<Result<object?>> DeleteFile([FromQuery] string filePath, CancellationToken cancellationToken)
    {
        var ok = await _fileService.DeleteAsync(filePath, cancellationToken);
        return Result.Judge(ok);
    }

    public sealed class FileUploadRequest
    {
        public IFormFile? File { get; init; }
    }
}
