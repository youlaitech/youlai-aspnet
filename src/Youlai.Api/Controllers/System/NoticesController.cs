using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Notice;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 閫氱煡鍏憡鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵鍏憡鐨勫垎椤垫煡璇€佽鎯呫€佸彂甯冧笌鎾ゅ洖绛夎兘鍔?
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
    /// 鍏憡鍒嗛〉
    /// </summary>
    [HttpGet]
    [HasPerm("sys:notice:list")]
    public Task<PageResult<NoticePageVo>> GetNoticePage([FromQuery] NoticeQuery query, CancellationToken cancellationToken)
    {
        return _noticeService.GetNoticePageAsync(query, cancellationToken);
    }

    /// <summary>
    /// 鍏憡琛ㄥ崟
    /// </summary>
    [HttpGet("{id:long}/form")]
    [HasPerm("sys:notice:update")]
    public async Task<Result<NoticeForm>> GetNoticeForm([FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _noticeService.GetNoticeFormAsync(id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 鏂板鍏憡
    /// </summary>
    [HttpPost]
    [HasPerm("sys:notice:create")]
    public async Task<Result<object?>> CreateNotice([FromBody] NoticeForm formData, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.CreateNoticeAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鏇存柊鍏憡
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:notice:update")]
    public async Task<Result<object?>> UpdateNotice([FromRoute] long id, [FromBody] NoticeForm formData, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.UpdateNoticeAsync(id, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鎵归噺鍒犻櫎鍏憡
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("sys:notice:delete")]
    public async Task<Result<object?>> DeleteNotices([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.DeleteNoticesAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鍙戝竷鍏憡
    /// </summary>
    [HttpPut("{id:long}/publish")]
    [HasPerm("sys:notice:publish")]
    public async Task<Result<object?>> Publish([FromRoute] long id, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.PublishNoticeAsync(id, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鎾ゅ洖鍏憡
    /// </summary>
    [HttpPut("{id:long}/revoke")]
    [HasPerm("sys:notice:revoke")]
    public async Task<Result<object?>> Revoke([FromRoute] long id, CancellationToken cancellationToken)
    {
        var ok = await _noticeService.RevokeNoticeAsync(id, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鍏憡璇︽儏
    /// </summary>
    [HttpGet("{id:long}/detail")]
    public async Task<Result<NoticeDetailVo>> Detail([FromRoute] long id, CancellationToken cancellationToken)
    {
        var detail = await _noticeService.GetNoticeDetailAsync(id, cancellationToken);
        return Result.Success(detail);
    }

    /// <summary>
    /// 鍏ㄩ儴鏍囪宸茶
    /// </summary>
    [HttpPut("read-all")]
    public async Task<Result<object?>> ReadAll(CancellationToken cancellationToken)
    {
        var ok = await _noticeService.ReadAllAsync(cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鎴戠殑鍏憡鍒嗛〉
    /// </summary>
    [HttpGet("my")]
    public Task<PageResult<NoticePageVo>> GetMyNoticePage([FromQuery] NoticeQuery query, CancellationToken cancellationToken)
    {
        return _noticeService.GetMyNoticePageAsync(query, cancellationToken);
    }
}
