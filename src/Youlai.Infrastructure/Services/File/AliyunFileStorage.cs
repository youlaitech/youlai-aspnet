using System.Globalization;
using Aliyun.OSS;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.File.Dtos;
using Youlai.Infrastructure.Options;

namespace Youlai.Infrastructure.Services.File;

internal sealed class AliyunFileStorage : IFileStorage
{
    private readonly OssOptions.AliyunOptions _options;
    private readonly OssClient _client;

    public AliyunFileStorage(OssOptions options)
    {
        _options = options.Aliyun;
        _client = new OssClient(_options.Endpoint, _options.AccessKeyId, _options.AccessKeySecret);
    }

    public string Type => "aliyun";

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

    private static string BuildObjectName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var dateFolder = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        return $"{dateFolder}/{storedFileName}";
    }

    private string ParseObjectName(string filePath)
    {
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        return filePath.TrimStart('/');
    }
}
