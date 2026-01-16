using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;

namespace Youlai.Api.Controllers.Common;

/// <summary>
/// 鍋ュ悍妫€鏌ユ帴鍙?
/// </summary>
/// <remarks>
/// 鎻愪緵鏈嶅姟鍙敤鎬ф鏌?
/// </remarks>
[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// 鍋ュ悍妫€鏌?
    /// </summary>
    [HttpGet]
    public Result<string> Get()
    {
        return Result<string>.Success("ok");
    }
}
