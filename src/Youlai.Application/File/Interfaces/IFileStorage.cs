using Youlai.Application.File.Dtos;

namespace Youlai.Application.File.Interfaces;

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
