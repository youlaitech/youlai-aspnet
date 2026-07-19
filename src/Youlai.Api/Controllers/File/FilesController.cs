using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Youlai.Application.Exceptions;
using Youlai.Application.Options;
using Youlai.Application.Results;
using Youlai.Application.File;

namespace Youlai.Api.Controllers.File;

/// <summary>
/// 文件接口
/// </summary>
[ApiController]
[Route("api/v1/files")]
[Authorize]
[Tags("10.文件接口")]
/// <summary>
/// 文件上传与下载接口
/// </summary>
public sealed class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly OssOptions _ossOptions;

    public FilesController(IFileService fileService, IOptions<OssOptions> ossOptions)
    {
        _fileService = fileService;
        _ossOptions = ossOptions.Value;
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

        ValidateUpload(file);

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

    /// <summary>
    /// 上传校验：扩展名白名单（为空不限制）与单文件大小上限
    /// </summary>
    private void ValidateUpload(IFormFile file)
    {
        var upload = _ossOptions.Upload;

        var allowed = upload.AllowedExtensionSet;
        if (allowed.Count > 0)
        {
            var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
            {
                throw new BusinessException(ResultCode.UploadFileException,
                    "不支持的文件类型: " + (string.IsNullOrEmpty(ext) ? string.Empty : "." + ext));
            }
        }

        if (upload.MaxFileSizeMb > 0 && file.Length > upload.MaxFileSizeBytes)
        {
            throw new BusinessException(ResultCode.UploadFileException,
                "文件大小超过限制: " + upload.MaxFileSizeMb + "MB");
        }
    }

    /// <summary>
    /// 文件上传请求参数
    /// </summary>
    public sealed class FileUploadRequest
    {
        public IFormFile? File { get; init; }
    }
}
