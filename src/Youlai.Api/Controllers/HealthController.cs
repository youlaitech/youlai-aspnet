using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Results;

namespace Youlai.Api.Controllers;

/// <summary>
/// 系统健康检查接口
/// </summary>
[ApiController]
[Route("api/v1/health")]
[Tags("00.系统健康检查")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// 返回 ok，供限流验证与探针使用
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public Result<string> Health() => Result.Success("ok");
}
