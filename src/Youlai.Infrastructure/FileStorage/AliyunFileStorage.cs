using System.Globalization;
using Aliyun.OSS;
using Youlai.Application.Exceptions;
using Youlai.Application.Results;
using Youlai.Application.File;
using Youlai.Application.Options;

namespace Youlai.Infrastructure.FileStorage;

/// <summary>
/// 阿里云 OSS 文件存储实现
/// </summary>
internal sealed class AliyunFileStorage : IFileStorage
{
    private readonly FileStorageOptions.AliyunOptions _options;
    private readonly OssClient _client;

    public AliyunFileStorage(FileStorageOptions options)
    {
        _options = options.Aliyun;
        _client = new OssClient(_options.Endpoint, _options.AccessKeyId, _options.AccessKeySecret);
    }

    /// <summary>存储类型标识：aliyun</summary>
    public string Type => "aliyun";

    /// <summary>
    /// 上传对象到 OSS，返回可访问 URL；SDK 为同步接口，用 Task.Run 包装
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

        var objectName = BuildObjectName(fileName);
        try
        {
            var metadata = new ObjectMetadata
            {
                ContentType = contentType ?? "application/octet-stream",
            };

            var request = new PutObjectRequest(_options.BucketName, objectName, content, metadata);
            await Task.Run(() => _client.PutObject(request), cancellationToken);
        }
        catch (Exception ex)
        {
            throw new BusinessException(ResultCode.UploadFileException, "文件上传失败", ex);
        }

        var url = $"https://{_options.BucketName}.{_options.Endpoint.TrimEnd('/')}/{objectName}";
        return new FileInfoDto
        {
            Name = fileName,
            Url = url,
        };
    }

    /// <summary>
    /// 从 URL 或路径解析出对象名后删除
    /// </summary>
    public async Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new BusinessException(ResultCode.RequestRequiredParameterIsEmpty, "文件路径不能为空");
        }

        var objectName = ParseObjectName(filePath);
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new BusinessException(ResultCode.DeleteFileException, "无法解析文件路径");
        }

        try
        {
            await Task.Run(() => _client.DeleteObject(_options.BucketName, objectName), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            throw new BusinessException(ResultCode.DeleteFileException, "文件删除失败", ex);
        }
    }

    /// <summary>
    /// 生成对象名「yyyyMMdd/GUID.ext」
    /// </summary>
    private static string BuildObjectName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var dateFolder = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        return $"{dateFolder}/{storedFileName}";
    }

    /// <summary>
    /// 从 URL 或路径归一为对象名
    /// </summary>
    private string ParseObjectName(string filePath)
    {
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        return filePath.TrimStart('/');
    }
}
