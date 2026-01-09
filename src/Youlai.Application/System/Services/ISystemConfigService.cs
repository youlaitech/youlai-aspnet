using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos;

namespace Youlai.Application.System.Services;

/// <summary>
/// 参数配置
/// </summary>
public interface ISystemConfigService
{
    /// <summary>
    /// 分页查询配置
    /// </summary>
    Task<PageResult<ConfigPageVo>> GetConfigPageAsync(ConfigPageQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取配置表单
    /// </summary>
    Task<ConfigForm> GetConfigFormAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增配置
    /// </summary>
    Task<bool> SaveConfigAsync(ConfigForm form, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新配置
    /// </summary>
    Task<bool> UpdateConfigAsync(long id, ConfigForm form, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除配置
    /// </summary>
    Task<bool> DeleteConfigAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新配置缓存
    /// </summary>
    Task<bool> RefreshCacheAsync(CancellationToken cancellationToken = default);
}
