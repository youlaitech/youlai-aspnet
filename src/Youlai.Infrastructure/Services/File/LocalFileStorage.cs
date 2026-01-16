using System.Globalization;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.File.Dtos;
using Youlai.Infrastructure.Options;

namespace Youlai.Infrastructure.Services.File;

internal sealed class LocalFileStorage : IFileStorage
{
    private readonly OssOptions.LocalOptions _options;

    public LocalFileStorage(OssOptions options)
    {
        _options = options.Local;
    }

    public string Type => "local";

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
            throw new BusinessException(ResultCode.SystemError, "Oss:Local:StoragePath 未配置");
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

    private string BuildPublicUrl(string relativePath)
    {
        var normalized = relativePath.Replace("\\", "/").TrimStart('/');
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl!.TrimEnd('/')}/{normalized}";
        }

        return "/" + normalized;
    }

    private string NormalizePath(string filePath)
    {
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        return filePath.TrimStart('/').Replace("\\", "/");
    }
}
