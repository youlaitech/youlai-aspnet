using System.Globalization;
using Amazon.S3;
using Amazon.S3.Model;
using Youlai.Application.Exceptions;
using Youlai.Application.Results;
using Youlai.Application.File;
using Youlai.Application.Options;

namespace Youlai.Infrastructure.FileStorage;

/// <summary>
/// S3（RustFS）文件存储实现，通过 AWS SDK for .NET 接入 S3 兼容服务
/// </summary>
internal sealed class S3FileStorage : IFileStorage
{
    private readonly FileStorageOptions.S3Options _options;
    private readonly IAmazonS3 _client;
    private readonly string _publicEndpoint;

    public S3FileStorage(FileStorageOptions options)
    {
        _options = options.S3;
        _client = BuildClient(_options);
        _publicEndpoint = NormalizeEndpoint(_options.Endpoint);
    }

    /// <summary>存储类型标识：s3</summary>
    public string Type => "s3";

    /// <summary>
    /// 上传对象到 S3，桶不存在时自动创建，返回可访问 URL
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
            await EnsureBucketAsync(cancellationToken);

            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectName,
                InputStream = content,
                ContentType = contentType ?? "application/octet-stream",
                AutoCloseStream = false,
            };

            await _client.PutObjectAsync(putRequest, cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            throw new BusinessException(ResultCode.UploadFileException, "文件上传失败", ex);
        }
        catch (IOException ex)
        {
            throw new BusinessException(ResultCode.UploadFileException, "文件上传失败", ex);
        }

        var url = BuildPublicUrl(objectName);
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
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectName,
            };

            await _client.DeleteObjectAsync(deleteRequest, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            throw new BusinessException(ResultCode.DeleteFileException, "文件删除失败", ex);
        }
        catch (IOException ex)
        {
            throw new BusinessException(ResultCode.DeleteFileException, "文件删除失败", ex);
        }
    }

    /// <summary>
    /// 按 Endpoint 构建客户端（路径风格寻址，兼容 RustFS 等自托管 S3 服务）
    /// </summary>
    private static IAmazonS3 BuildClient(FileStorageOptions.S3Options options)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = true,
            RegionEndpoint = Amazon.RegionEndpoint.USEast1,
        };

        if (Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri))
        {
            config.ServiceURL = options.Endpoint;
        }
        else
        {
            config.ServiceURL = $"http://{options.Endpoint}";
        }

        return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }

    /// <summary>
    /// 归一 Endpoint：未带协议前缀时补 http://
    /// </summary>
    private static string NormalizeEndpoint(string endpoint)
    {
        if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint.TrimEnd('/');
        }

        return $"http://{endpoint.TrimEnd('/')}";
    }

    /// <summary>
    /// 生成对象名「yyyyMMdd/GUID.ext」
    /// </summary>
    private string BuildObjectName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var dateFolder = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        return $"{dateFolder}/{storedFileName}";
    }

    /// <summary>
    /// 桶不存在则创建
    /// </summary>
    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        try
        {
            // HEAD 桶语义：能取到 Location 即视为存在
            await _client.GetBucketLocationAsync(new GetBucketLocationRequest
            {
                BucketName = _options.BucketName,
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await _client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = _options.BucketName,
            }, cancellationToken);

            // 公开只读策略：匿名可读对象，禁止匿名写入
            await _client.PutBucketPolicyAsync(new PutBucketPolicyRequest
            {
                BucketName = _options.BucketName,
                Policy = BuildPublicReadPolicy(),
            }, cancellationToken);
        }
    }

    /// <summary>
    /// 桶的匿名只读策略（仅 GetObject）
    /// </summary>
    private string BuildPublicReadPolicy()
    {
        return "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\","
            + "\"Principal\":{\"AWS\":[\"*\"]},\"Action\":[\"s3:GetObject\"],"
            + "\"Resource\":[\"arn:aws:s3:::" + _options.BucketName + "/*\"]}]}";
    }

    /// <summary>
    /// 拼接对外访问 URL，优先用自定义域名
    /// </summary>
    private string BuildPublicUrl(string objectName)
    {
        if (!string.IsNullOrWhiteSpace(_options.CustomDomain))
        {
            return $"{_options.CustomDomain!.TrimEnd('/')}/{_options.BucketName}/{objectName}";
        }

        return $"{_publicEndpoint}/{_options.BucketName}/{objectName}";
    }

    /// <summary>
    /// 从 URL 或路径中剥离桶名，得到对象名
    /// </summary>
    private string ParseObjectName(string filePath)
    {
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath.TrimStart('/');
            if (path.StartsWith(_options.BucketName + "/", StringComparison.OrdinalIgnoreCase))
            {
                return path[(_options.BucketName.Length + 1)..];
            }

            return path;
        }

        var normalized = filePath.TrimStart('/');
        if (normalized.StartsWith(_options.BucketName + "/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized[(_options.BucketName.Length + 1)..];
        }

        return normalized;
    }
}
