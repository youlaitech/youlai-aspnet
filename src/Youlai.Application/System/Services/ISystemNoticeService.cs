using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Notice;

namespace Youlai.Application.System.Services;

/// <summary>
/// 通知公告
/// </summary>
public interface ISystemNoticeService
{
    /// <summary>
    /// 分页查询公告
    /// </summary>
    Task<PageResult<NoticePageVo>> GetNoticePageAsync(NoticeQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取公告表单
    /// </summary>
    Task<NoticeForm> GetNoticeFormAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增公告
    /// </summary>
    Task<bool> CreateNoticeAsync(NoticeForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新公告
    /// </summary>
    Task<bool> UpdateNoticeAsync(long id, NoticeForm formData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除公告
    /// </summary>
    Task<bool> DeleteNoticesAsync(string ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布公告
    /// </summary>
    Task<bool> PublishNoticeAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回公告
    /// </summary>
    Task<bool> RevokeNoticeAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 公告详情
    /// </summary>
    Task<NoticeDetailVo> GetNoticeDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全部标记已读
    /// </summary>
    Task<bool> ReadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 我的公告分页
    /// </summary>
    Task<PageResult<NoticePageVo>> GetMyNoticePageAsync(NoticeQuery query, CancellationToken cancellationToken = default);
}
