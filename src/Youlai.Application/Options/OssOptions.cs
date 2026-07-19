namespace Youlai.Application.Options;

/// <summary>
/// 对象存储配置
/// </summary>
public sealed class OssOptions
{
    public const string SectionName = "Oss";

    public string Type { get; init; } = "local";

    public UploadOptions Upload { get; init; } = new();

    public LocalOptions Local { get; init; } = new();

    public MinioOptions Minio { get; init; } = new();

    public AliyunOptions Aliyun { get; init; } = new();

    /// <summary>
    /// 上传限制
    /// </summary>
    public sealed class UploadOptions
    {
        /// <summary>
        /// 单文件大小上限（单位：MB）
        /// </summary>
        public int MaxFileSizeMb { get; init; } = 50;

        /// <summary>
        /// 允许的文件扩展名白名单，逗号分隔（置空表示不限制）
        /// </summary>
        public string AllowedExtensions { get; init; } = "jpg,jpeg,png,gif";

        /// <summary>
        /// 单文件大小上限（字节）
        /// </summary>
        public long MaxFileSizeBytes => (long)MaxFileSizeMb * 1024 * 1024;

        /// <summary>
        /// 允许扩展名集合（小写、不含点）；空集合表示不限制
        /// </summary>
        public IReadOnlySet<string> AllowedExtensionSet =>
            (AllowedExtensions ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.StartsWith('.') ? e[1..] : e)
                .Select(e => e.ToLowerInvariant())
                .ToHashSet();
    }

    /// <summary>
    /// 本地文件存储配置项
    /// </summary>
    public sealed class LocalOptions
    {
        public string StoragePath { get; init; } = string.Empty;

        public string? PublicBaseUrl { get; init; }
    }

    /// <summary>
    /// MinIO 文件存储配置项
    /// </summary>
    public sealed class MinioOptions
    {
        public string Endpoint { get; init; } = string.Empty;

        public string AccessKey { get; init; } = string.Empty;

        public string SecretKey { get; init; } = string.Empty;

        public string BucketName { get; init; } = string.Empty;

        public string? CustomDomain { get; init; }
    }

    /// <summary>
    /// 阿里云 OSS 配置项
    /// </summary>
    public sealed class AliyunOptions
    {
        public string Endpoint { get; init; } = string.Empty;

        public string AccessKeyId { get; init; } = string.Empty;

        public string AccessKeySecret { get; init; } = string.Empty;

        public string BucketName { get; init; } = string.Empty;
    }
}
