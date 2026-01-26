using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;

namespace Youlai.Api.Controllers.Common;

/// <summary>
/// 健康检查接口
/// </summary>
/// <remarks>
/// 提供服务可用性检测。
/// </remarks>
[ApiController]
[Route("api/v1/health")]
[Tags("98.基础能力")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet]
    public Result<string> Get()
    {
        return Result<string>.Success("ok");
    }
}
