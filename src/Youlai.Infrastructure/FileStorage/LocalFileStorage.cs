using System.Globalization;
using Youlai.Application.Exceptions;
using Youlai.Application.Results;
using Youlai.Application.File;
using Youlai.Application.Options;

namespace Youlai.Infrastructure.FileStorage;

/// <summary>
/// 本地磁盘文件存储实现
/// </summary>
internal sealed class LocalFileStorage : IFileStorage
{
    private readonly FileStorageOptions.LocalOptions _options;

    public LocalFileStorage(FileStorageOptions options)
    {
        _options = options.Local;
    }

    /// <summary>存储类型标识：local</summary>
    public string Type => "local";

    /// <summary>
    /// 保存到「storagePath/yyyyMMdd/GUID.ext」，返回可访问 URL
    /// </summary>
    public async Task<FileInfoDto> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long contentLength,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new BusinessException(ResultCode.RequestRequiredParameterIsEmpty, "文件名不能为空");
        }

        var extension = Path.GetExtension(fileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? string.Empty : extension;
        var dateFolder = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";

        var relativePath = Path.Combine(dateFolder, storedFileName);
        var storagePath = _options.StoragePath.Trim();
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new BusinessException(ResultCode.SystemError, "FileStorage:Local:StoragePath 未配置");
        }

        var absolutePath = Path.Combine(storagePath, relativePath);
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            await using var fileStream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await content.CopyToAsync(fileStream, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new BusinessException(ResultCode.UploadFileException, "文件上传失败", ex);
        }

        var url = BuildPublicUrl(relativePath);
        return new FileInfoDto
        {
            Name = fileName,
            Url = url,
        };
    }

    /// <summary>
    /// 删除文件，校验路径未越出存储根目录防止路径穿越
    /// </summary>
    public Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new BusinessException(ResultCode.RequestRequiredParameterIsEmpty, "文件路径不能为空");
        }

        var relativePath = NormalizePath(filePath);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new BusinessException(ResultCode.DeleteFileException, "无法解析文件路径");
        }

        var storagePath = _options.StoragePath.Trim();
        var absolutePath = Path.GetFullPath(Path.Combine(storagePath, relativePath));
        var rootPath = Path.GetFullPath(storagePath);
        if (!absolutePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(ResultCode.DeleteFileException, "文件路径不合法");
        }

        try
        {
            if (!System.IO.File.Exists(absolutePath))
            {
                return Task.FromResult(false);
            }

            System.IO.File.Delete(absolutePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            throw new BusinessException(ResultCode.DeleteFileException, "文件删除失败", ex);
        }
    }

    /// <summary>
    /// 拼接对外访问 URL，配置了 PublicBaseUrl 用它，否则用站内相对路径
    /// </summary>
    private string BuildPublicUrl(string relativePath)
    {
        var normalized = relativePath.Replace("\\", "/").TrimStart('/');
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl!.TrimEnd('/')}/{normalized}";
        }

        return "/" + normalized;
    }

    /// <summary>
    /// 把完整 URL 或路径归一为相对存储路径
    /// </summary>
    private string NormalizePath(string filePath)
    {
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        return filePath.TrimStart('/').Replace("\\", "/");
    }
}
