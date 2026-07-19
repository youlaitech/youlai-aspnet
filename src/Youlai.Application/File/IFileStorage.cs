namespace Youlai.Application.File;

/// <summary>
/// 文件存储抽象：屏蔽本地 / MinIO / 阿里云差异
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// 存储类型标识（local/minio/aliyun）
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
