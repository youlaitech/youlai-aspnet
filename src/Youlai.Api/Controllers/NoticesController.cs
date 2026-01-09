using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers;

/// <summary>
/// 通知公告接口
/// </summary>
/// <remarks>
/// 提供公告的分页查询、详情、发布与撤回等能力
/// </remarks>
[ApiController]
[Route("api/v1/notices")]
[Authorize]
public sealed class NoticesController : ControllerBase
{
    private readonly ISystemNoticeService _noticeService;

    public NoticesController(ISystemNoticeService noticeService)
    {
        _noticeService = noticeService;
    }

    /// <summary>
    /// 公告分页
    /// </summary>
    [HttpGet]
    [HasPerm("sys:notice:list")]
    public Task<PageResult<NoticePageVo>> GetNoticePage([FromQuery] NoticePageQuery query, CancellationToken cancellationToken)
    {
        return _noticeService.GetNoticePageAsync(query, cancellationToken);
    }

    /// <summary>
    /// 公告表单
    /// </summary>
    [HttpGet("{id:long}/form")]
    [HasPerm("sys:notice:update")]
    public async Task<Result<NoticeForm>> GetNoticeForm([FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _noticeService.GetNoticeFormAsync(id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 新增公告
    /// </summary>
    [HttpPost]
    [HasPerm("sys:notice:create")]
    public async Task<Result<object?>> CreateNotice([FromBody] NoticeForm formData, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.CreateNoticeAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 更新公告
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:notice:update")]
    public async Task<Result<object?>> UpdateNotice([FromRoute] long id, [FromBody] NoticeForm formData, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.UpdateNoticeAsync(id, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 批量删除公告
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("sys:notice:delete")]
    public async Task<Result<object?>> DeleteNotices([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.DeleteNoticesAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 发布公告
    /// </summary>
    [HttpPut("{id:long}/publish")]
    [HasPerm("sys:notice:publish")]
    public async Task<Result<object?>> Publish([FromRoute] long id, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.PublishNoticeAsync(id, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 撤回公告
    /// </summary>
    [HttpPut("{id:long}/revoke")]
    [HasPerm("sys:notice:revoke")]
    public async Task<Result<object?>> Revoke([FromRoute] long id, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.RevokeNoticeAsync(id, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 公告详情
    /// </summary>
    [HttpGet("{id:long}/detail")]
    public async Task<Result<NoticeDetailVo>> Detail([FromRoute] long id, CancellationToken cancellationToken)
    {
        var detail = await _noticeService.GetNoticeDetailAsync(id, cancellationToken);
        return Result.Success(detail);
    }

    /// <summary>
    /// 全部标记已读
    /// </summary>
    [HttpPut("read-all")]
    public async Task<Result<object?>> ReadAll(CancellationToken cancellationToken)
    {
        var ok = await _noticeService.ReadAllAsync(cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 我的公告分页
    /// </summary>
    [HttpGet("my")]
    public Task<PageResult<NoticePageVo>> GetMyNoticePage([FromQuery] NoticePageQuery query, CancellationToken cancellationToken)
    {
        return _noticeService.GetMyNoticePageAsync(query, cancellationToken);
    }
}
