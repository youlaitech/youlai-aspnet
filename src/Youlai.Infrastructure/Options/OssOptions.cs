namespace Youlai.Infrastructure.Options;

/// <summary>
/// 对象存储配置
/// </summary>
public sealed class OssOptions
{
    public const string SectionName = "Oss";

    public string Type { get; init; } = "local";

    public LocalOptions Local { get; init; } = new();

    public MinioOptions Minio { get; init; } = new();

    public AliyunOptions Aliyun { get; init; } = new();

    public sealed class LocalOptions
    {
        public string StoragePath { get; init; } = string.Empty;

        public string? PublicBaseUrl { get; init; }
    }

    public sealed class MinioOptions
    {
        public string Endpoint { get; init; } = string.Empty;

        public string AccessKey { get; init; } = string.Empty;

        public string SecretKey { get; init; } = string.Empty;

        public string BucketName { get; init; } = string.Empty;

        public string? CustomDomain { get; init; }
    }

    public sealed class AliyunOptions
    {
        public string Endpoint { get; init; } = string.Empty;

        public string AccessKeyId { get; init; } = string.Empty;

        public string AccessKeySecret { get; init; } = string.Empty;

        public string BucketName { get; init; } = string.Empty;
    }
}
