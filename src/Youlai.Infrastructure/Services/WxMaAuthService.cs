using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Youlai.Core.Auth.Dtos;
using Youlai.Core.Auth.Services;
using Youlai.Core.Exceptions;
using Youlai.Core.Results;
using Youlai.Core.Security;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Constants;
using Youlai.Infrastructure.Options;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 微信小程序认证服务实现
/// </summary>
internal sealed class WxMaAuthService : IWxMaAuthService
{
    private const string JsCode2SessionUrl = "https://api.weixin.qq.com/sns/jscode2session?appid={0}&secret={1}&js_code={2}&grant_type=authorization_code";
    private const string GetPhoneNumberUrl = "https://api.weixin.qq.com/wxa/business/getuserphonenumber?access_token={0}&code={1}";
    private const string GetAccessTokenUrl = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}";

    private static readonly TimeSpan SmsCodeTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly YoulaiDbContext _dbContext;
    private readonly JwtTokenManager _tokenManager;
    private readonly WechatMiniappOptions _options;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WxMaAuthService> _logger;

    public WxMaAuthService(
        YoulaiDbContext dbContext,
        JwtTokenManager tokenManager,
        IOptions<WechatMiniappOptions> options,
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory,
        ILogger<WxMaAuthService> logger)
    {
        _dbContext = dbContext;
        _tokenManager = tokenManager;
        _options = options.Value;
        _redis = redis;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 静默登录
    /// </summary>
    public async Task<WxMaLoginResultDto> SilentLoginAsync(string code, CancellationToken cancellationToken = default)
    {
        // 1. 获取微信会话信息
        var session = await GetSessionAsync(code, cancellationToken);
        var openId = session.OpenId;

        if (string.IsNullOrWhiteSpace(openId))
        {
            throw new BusinessException(ResultCode.UserLoginException, "微信登录失败：无法获取用户标识");
        }

        // 2. 查找是否已绑定用户
        var social = await _dbContext.SysUserSocials
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Platform == "WECHAT_MINI" && s.OpenId == openId, cancellationToken);

        if (social is not null)
        {
            // 已绑定用户，直接登录
            var token = await GenerateTokenByUserIdAsync(social.UserId, cancellationToken);
            return WxMaLoginResultDto.Success(token);
        }

        // 未绑定用户，返回需要绑定手机号
        _logger.LogInformation("微信小程序静默登录：用户未绑定手机号，openId={OpenId}", openId);
        return WxMaLoginResultDto.RequireBindMobile(openId);
    }

    /// <summary>
    /// 手机号快捷登录
    /// </summary>
    public async Task<AuthenticationTokenDto> PhoneLoginAsync(string loginCode, string phoneCode, CancellationToken cancellationToken = default)
    {
        // 1. 获取微信会话信息
        var session = await GetSessionAsync(loginCode, cancellationToken);
        var openId = session.OpenId;

        if (string.IsNullOrWhiteSpace(openId))
        {
            throw new BusinessException(ResultCode.UserLoginException, "微信登录失败：无法获取用户标识");
        }

        var nonNullOpenId = openId;

        // 2. 获取手机号
        var mobile = await GetPhoneNumberAsync(phoneCode, cancellationToken);

        _logger.LogInformation("微信小程序手机号快捷登录：openId={OpenId}, mobile={Mobile}", openId, mobile);

        // 3. 查询或创建用户
        var user = await FindOrCreateUserAsync(mobile, cancellationToken);

        // 4. 绑定微信 openid
        await BindWechatOpenIdAsync(user.Id, nonNullOpenId, session.UnionId, session.SessionKey, cancellationToken);

        // 5. 生成认证令牌
        return await GenerateTokenByUserIdAsync(user.Id, cancellationToken);
    }

    /// <summary>
    /// 绑定手机号
    /// </summary>
    public async Task<AuthenticationTokenDto> BindMobileAsync(string openId, string mobile, string smsCode, CancellationToken cancellationToken = default)
    {
        // 1. 验证短信验证码
        await ValidateSmsCodeAsync(mobile, smsCode);

        // 2. 查询或创建用户
        var user = await FindOrCreateUserAsync(mobile, cancellationToken);

        // 3. 绑定微信 openid
        await BindWechatOpenIdAsync(user.Id, openId, null, null, cancellationToken);

        _logger.LogInformation("微信小程序绑定手机号成功：mobile={Mobile}, openId={OpenId}", mobile, openId);

        // 4. 生成认证令牌
        return await GenerateTokenByUserIdAsync(user.Id, cancellationToken);
    }

    // ==================== 私有方法 ====================

    private async Task<WechatSessionResponse> GetSessionAsync(string code, CancellationToken cancellationToken)
    {
        var url = string.Format(JsCode2SessionUrl, _options.AppId, _options.AppSecret, code);
        var httpClient = _httpClientFactory.CreateClient("Wechat");

        try
        {
            var response = await httpClient.GetFromJsonAsync<WechatSessionResponse>(url, JsonOptions, cancellationToken);
            if (response is null || !string.IsNullOrEmpty(response.ErrCode))
            {
                var errMsg = response?.ErrMsg ?? "Unknown error";
                _logger.LogError("获取微信会话信息失败：code={Code}, errCode={ErrCode}, errMsg={ErrMsg}", code, response?.ErrCode, errMsg);
                throw new BusinessException(ResultCode.UserLoginException, $"微信登录失败：{errMsg}");
            }

            return response;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取微信会话信息失败，code={Code}", code);
            throw new BusinessException(ResultCode.UserLoginException, "微信登录失败：" + ex.Message);
        }
    }

    private async Task<string> GetPhoneNumberAsync(string phoneCode, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var url = string.Format(GetPhoneNumberUrl, accessToken, phoneCode);
        var httpClient = _httpClientFactory.CreateClient("Wechat");

        try
        {
            var response = await httpClient.GetFromJsonAsync<WechatPhoneNumberResponse>(url, JsonOptions, cancellationToken);
            if (response is null || response.ErrCode != 0)
            {
                var errMsg = response?.ErrMsg ?? "Unknown error";
                _logger.LogError("获取微信手机号失败：phoneCode={PhoneCode}, errCode={ErrCode}, errMsg={ErrMsg}", phoneCode, response?.ErrCode, errMsg);
                throw new BusinessException(ResultCode.UserLoginException, $"获取手机号失败：{errMsg}");
            }

            return response.PhoneInfo?.PhoneNumber ?? throw new BusinessException(ResultCode.UserLoginException, "获取手机号失败");
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取微信手机号失败，phoneCode={PhoneCode}", phoneCode);
            throw new BusinessException(ResultCode.UserLoginException, "获取手机号失败：" + ex.Message);
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var cacheKey = string.Format(RedisKeyConstants.Wechat.AccessToken, _options.AppId);

        // 先从缓存获取
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            return cached.ToString();
        }

        // 请求新 token
        var url = string.Format(GetAccessTokenUrl, _options.AppId, _options.AppSecret);
        var httpClient = _httpClientFactory.CreateClient("Wechat");

        var response = await httpClient.GetFromJsonAsync<WechatAccessTokenResponse>(url, JsonOptions, cancellationToken);
        if (response is null || !string.IsNullOrEmpty(response.ErrCode))
        {
            var errMsg = response?.ErrMsg ?? "Unknown error";
            throw new BusinessException(ResultCode.UserLoginException, $"获取微信AccessToken失败：{errMsg}");
        }

        // 缓存 token（提前5分钟过期）
        var expiresIn = Math.Max(response.ExpiresIn - 300, 60);
        await db.StringSetAsync(cacheKey, response.AccessToken, TimeSpan.FromSeconds(expiresIn));

        return response.AccessToken;
    }

    private async Task<SysUser> FindOrCreateUserAsync(string mobile, CancellationToken cancellationToken)
    {
        var user = await _dbContext.SysUsers
            .FirstOrDefaultAsync(u => u.Mobile == mobile && !u.IsDeleted, cancellationToken);

        if (user is not null)
        {
            return user;
        }

        // 创建新用户
        user = new SysUser
        {
            Username = "wx_" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Nickname = "微信用户",
            Mobile = mobile,
            Status = 1,
            IsDeleted = false,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        _dbContext.SysUsers.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 分配默认角色
        if (_options.DefaultRoleId > 0)
        {
            _dbContext.SysUserRoles.Add(new SysUserRole
            {
                UserId = user.Id,
                RoleId = _options.DefaultRoleId
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("微信小程序登录：创建新用户，mobile={Mobile}, userId={UserId}", mobile, user.Id);

        return user;
    }

    private async Task BindWechatOpenIdAsync(long userId, string openId, string? unionId, string? sessionKey, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _dbContext.SysUserSocials
                .FirstOrDefaultAsync(s => s.Platform == "WECHAT_MINI" && s.OpenId == openId, cancellationToken);

            if (existing is not null)
            {
                // 更新绑定
                existing.UserId = userId;
                existing.UnionId = unionId;
                existing.SessionKey = sessionKey;
                existing.UpdateTime = DateTime.Now;
            }
            else
            {
                // 新增绑定
                _dbContext.SysUserSocials.Add(new SysUserSocial
                {
                    UserId = userId,
                    Platform = "WECHAT_MINI",
                    OpenId = openId,
                    UnionId = unionId,
                    SessionKey = sessionKey,
                    Verified = true,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // 绑定失败不影响登录
            _logger.LogWarning(ex, "绑定微信 openid 失败，userId={UserId}, openId={OpenId}", userId, openId);
        }
    }

    private async Task ValidateSmsCodeAsync(string mobile, string smsCode)
    {
        var db = _redis.GetDatabase();
        var cacheKey = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile.Trim());
        var cached = await db.StringGetAsync(cacheKey);

        if (!cached.HasValue)
        {
            throw new BusinessException(ResultCode.UserVerificationCodeExpired);
        }

        if (!string.Equals(cached.ToString(), smsCode, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        // 验证成功后删除验证码
        await db.KeyDeleteAsync(cacheKey);
    }

    private async Task<AuthenticationTokenDto> GenerateTokenByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new { u.Id, u.Username, u.DeptId })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new BusinessException(ResultCode.UserLoginException, "用户不存在");
        }

        // 查询用户的所有角色及其数据权限
        var rolesQuery =
            from ur in _dbContext.SysUserRoles.AsNoTracking()
            join r in _dbContext.SysRoles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == user.Id && !r.IsDeleted && r.Status == 1
            select new { r.Id, r.Code, r.DataScope };

        var roles = await rolesQuery.ToListAsync(cancellationToken);

        // 构建角色权限集合
        var authorities = roles
            .Select(r => r.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => SecurityConstants.RolePrefix + c)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // 构建数据权限列表
        var dataScopes = new List<RoleDataScope>();
        foreach (var role in roles)
        {
            var roleDataScope = new RoleDataScope
            {
                RoleCode = role.Code ?? string.Empty,
                DataScope = role.DataScope ?? 4
            };

            if (role.DataScope == 5 && role.Id != 0)
            {
                var customDeptIds = await _dbContext.SysRoleDepts
                    .AsNoTracking()
                    .Where(rd => rd.RoleId == role.Id)
                    .Select(rd => rd.DeptId)
                    .ToListAsync(cancellationToken);

                roleDataScope.CustomDeptIds = customDeptIds;
            }

            dataScopes.Add(roleDataScope);
        }

        var subject = new JwtTokenManager.AuthTokenSubject(
            UserId: user.Id,
            DeptId: user.DeptId ?? 0,
            DataScopes: dataScopes,
            Username: user.Username ?? string.Empty,
            Authorities: authorities
        );

        return _tokenManager.GenerateToken(subject);
    }

    #region 微信 API 响应模型

    private sealed class WechatSessionResponse
    {
        [JsonPropertyName("openid")]
        public string? OpenId { get; set; }

        [JsonPropertyName("session_key")]
        public string? SessionKey { get; set; }

        [JsonPropertyName("unionid")]
        public string? UnionId { get; set; }

        [JsonPropertyName("errcode")]
        public string? ErrCode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? ErrMsg { get; set; }
    }

    private sealed class WechatAccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("errcode")]
        public string? ErrCode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? ErrMsg { get; set; }
    }

    private sealed class WechatPhoneNumberResponse
    {
        [JsonPropertyName("errcode")]
        public int ErrCode { get; set; }

        [JsonPropertyName("errmsg")]
        public string ErrMsg { get; set; } = string.Empty;

        [JsonPropertyName("phone_info")]
        public WechatPhoneInfo? PhoneInfo { get; set; }
    }

    private sealed class WechatPhoneInfo
    {
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("purePhoneNumber")]
        public string? PurePhoneNumber { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }
    }

    #endregion
}
