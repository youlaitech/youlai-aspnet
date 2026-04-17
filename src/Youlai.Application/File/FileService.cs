namespace Youlai.Application.File;

/// <summary>
/// 文件上传/删除
/// </summary>
internal sealed class FileService : IFileService
{
    private readonly IFileStorage _storage;

    public FileService(IFileStorage storage)
    {
        _storage = storage;
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
        return _storage.UploadAsync(content, fileName, contentType, contentLength, cancellationToken);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return _storage.DeleteAsync(filePath, cancellationToken);
    }
}
