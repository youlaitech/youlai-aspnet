using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Results;
using Youlai.Application.Platform.File.Dtos;
using Youlai.Application.Platform.File.Services;
using Youlai.Infrastructure.Options;

namespace Youlai.Infrastructure.Services.File;

internal sealed class FileService : IFileService
{
    private readonly IReadOnlyCollection<IFileStorage> _storages;
    private readonly OssOptions _options;

    public FileService(IEnumerable<IFileStorage> storages, OssOptions options)
    {
        _storages = storages.ToList();
        _options = options;
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    public Task<FileInfoDto> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long contentLength,
        CancellationToken cancellationToken = default)
    {
        var storage = ResolveStorage();
        return storage.UploadAsync(content, fileName, contentType, contentLength, cancellationToken);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var storage = ResolveStorage();
        return storage.DeleteAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// 根据 Oss:Type 选择存储实现
    /// </summary>
    private IFileStorage ResolveStorage()
    {
        var type = _options.Type?.Trim();
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new BusinessException(ResultCode.SystemError, "Oss:Type 未配置");
        }

        var storage = _storages.FirstOrDefault(item => item.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (storage is null)
        {
            throw new BusinessException(ResultCode.SystemError, $"未找到文件存储实现: {type}");
        }

        return storage;
    }
}
