namespace Youlai.Application.File;

/// <summary>
/// 文件上传/删除接口（本地/S3(RustFS)/阿里云）
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// 存储类型标识（local/s3/aliyun）
    /// </summary>
    string Type { get; }

    /// <summary>
    /// 上传文件
    /// </summary>
    Task<FileInfoDto> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long contentLength,
        CancellationToken cancellationToken);

    /// <summary>
    /// 删除文件
    /// </summary>
    Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken);
}
