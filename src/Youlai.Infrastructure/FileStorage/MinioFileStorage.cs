using System.Globalization;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Youlai.Application.Exceptions;
using Youlai.Application.Results;
using Youlai.Application.File;
using Youlai.Application.Options;

namespace Youlai.Infrastructure.FileStorage;

/// <summary>
/// MinIO 文件存储实现
/// </summary>
internal sealed class MinioFileStorage : IFileStorage
{
    private readonly OssOptions.MinioOptions _options;
    private readonly IMinioClient _client;
    private readonly string _publicEndpoint;

    public MinioFileStorage(OssOptions options)
    {
        _options = options.Minio;
        _client = BuildClient(_options);
        _publicEndpoint = NormalizeEndpoint(_options.Endpoint);
    }

    /// <summary>存储类型标识：minio</summary>
    public string Type => "minio";

    /// <summary>
    /// 上传对象到 MinIO，桶不存在时自动创建，返回可访问 URL
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

            var putArgs = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectName)
                .WithStreamData(content)
                .WithObjectSize(contentLength)
                .WithContentType(contentType ?? "application/octet-stream");

            await _client.PutObjectAsync(putArgs, cancellationToken);
        }
        catch (Exception ex) when (ex is MinioException or IOException)
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
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectName);

            await _client.RemoveObjectAsync(removeArgs, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is MinioException or IOException)
        {
            throw new BusinessException(ResultCode.DeleteFileException, "文件删除失败", ex);
        }
    }

    /// <summary>
    /// 按 Endpoint 构建客户端，https 前缀时启用 SSL
    /// </summary>
    private static IMinioClient BuildClient(OssOptions.MinioOptions options)
    {
        var builder = new MinioClient();
        if (Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            int? port = uri.IsDefaultPort ? null : uri.Port;
            if (port.HasValue)
            {
                builder.WithEndpoint(host, port.Value);
            }
            else
            {
                builder.WithEndpoint(host);
            }

            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                builder.WithSSL();
            }
        }
        else
        {
            builder.WithEndpoint(options.Endpoint);
        }

        return builder
            .WithCredentials(options.AccessKey, options.SecretKey)
            .Build();
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
        var existsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
        var exists = await _client.BucketExistsAsync(existsArgs, cancellationToken);
        if (exists)
        {
            return;
        }

        var makeArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
        await _client.MakeBucketAsync(makeArgs, cancellationToken);
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
