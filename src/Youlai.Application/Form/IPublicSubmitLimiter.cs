namespace Youlai.Application.Form;

/// <summary>
/// 公开表单提交限流端口（应用层不依赖具体 Redis 客户端实现）
/// </summary>
public interface IPublicSubmitLimiter
{
    /// <summary>
    /// 按 formKey + IP 维度计数并判断本次提交是否放行
    /// </summary>
    Task<bool> IsAllowedAsync(string formKey, string ip, CancellationToken cancellationToken = default);
}
