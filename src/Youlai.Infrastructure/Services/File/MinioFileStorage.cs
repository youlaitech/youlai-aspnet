using System.Globalization;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.File.Dtos;
using Youlai.Infrastructure.Options;

namespace Youlai.Infrastructure.Services.File;

internal sealed class MinioFileStorage : IFileStorage
{
    private readonly OssOptions.MinioOptions _options;
    private readonly MinioClient _client;
    private readonly string _publicEndpoint;

    public MinioFileStorage(OssOptions options)
    {
        _options = options.Minio;
        _client = BuildClient(_options);
        _publicEndpoint = NormalizeEndpoint(_options.Endpoint);
    }

    public string Type => "minio";

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

    private static MinioClient BuildClient(OssOptions.MinioOptions options)
    {
        var builder = new MinioClient();
        if (Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            var port = uri.IsDefaultPort ? null : uri.Port;
            builder = port.HasValue ? builder.WithEndpoint(host, port.Value) : builder.WithEndpoint(host);
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                builder = builder.WithSSL();
            }
        }
        else
        {
            builder = builder.WithEndpoint(options.Endpoint);
        }

        return builder
            .WithCredentials(options.AccessKey, options.SecretKey)
            .Build();
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint.TrimEnd('/');
        }

        return $"http://{endpoint.TrimEnd('/')}";
    }

    private string BuildObjectName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var dateFolder = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        return $"{dateFolder}/{storedFileName}";
    }

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

    private string BuildPublicUrl(string objectName)
    {
        if (!string.IsNullOrWhiteSpace(_options.CustomDomain))
        {
            return $"{_options.CustomDomain!.TrimEnd('/')}/{_options.BucketName}/{objectName}";
        }

        return $"{_publicEndpoint}/{_options.BucketName}/{objectName}";
    }

    private string ParseObjectName(string filePath)
    {
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath.TrimStart('/');
            if (path.StartsWith(_options.BucketName + "/", StringComparison.OrdinalIgnoreCase))
            {
                return path[_options.BucketName.Length + 1..];
            }

            return path;
        }

        var normalized = filePath.TrimStart('/');
        if (normalized.StartsWith(_options.BucketName + "/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized[_options.BucketName.Length + 1..];
        }

        return normalized;
    }
}
