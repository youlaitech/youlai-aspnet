using Youlai.Application.Platform.File.Dtos;

namespace Youlai.Application.Platform.File.Services;

/// <summary>
/// 文件服务
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="content">文件内容流</param>
    /// <param name="fileName">原始文件名</param>
    /// <param name="contentType">Content-Type</param>
    /// <param name="contentLength">文件大小（字节）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<FileInfoDto> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long contentLength,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="filePath">文件路径或完整 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default);
}
