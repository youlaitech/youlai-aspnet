using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Youlai.Application.Auth.Models;
using Youlai.Application.Constants;
using Youlai.Application.Exceptions;
using Youlai.Application.Options;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Application.Security.Models;
using Youlai.Domain.Entities;

namespace Youlai.Application.Auth;

/// <summary>
/// 微信小程序认证服务实现
/// </summary>
internal sealed class WxMaAuthService : IWxMaAuthService
{
    private const string JsCode2SessionUrl = "https://api.weixin.qq.com/sns/jscode2session?appid={0}&secret={1}&js_code={2}&grant_type=authorization_code";
    private const string GetPhoneNumberUrl = "https://api.weixin.qq.com/wxa/business/getuserphonenumber?access_token={0}&code={1}";
    private const string GetAccessTokenUrl = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}";
    private const string WechatProvider = "wechat-mini-main";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IReadOnlyDbContext _readDb;
    private readonly IRepository<SysUser> _userRepo;
    private readonly IRepository<SysUserIdentity> _userIdentityRepo;
    private readonly IRepository<SysUserRole> _userRoleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenManager _tokenManager;
    private readonly WxMaOptions _options;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WxMaAuthService> _logger;

    public WxMaAuthService(
        IReadOnlyDbContext readDb,
        IRepository<SysUser> userRepo,
        IRepository<SysUserIdentity> userIdentityRepo,
        IRepository<SysUserRole> userRoleRepo,
        IUnitOfWork unitOfWork,
        ITokenManager tokenManager,
        IOptions<WxMaOptions> options,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<WxMaAuthService> logger)
    {
        _readDb = readDb;
        _userRepo = userRepo;
        _userIdentityRepo = userIdentityRepo;
        _userRoleRepo = userRoleRepo;
        _unitOfWork = unitOfWork;
        _tokenManager = tokenManager;
        _options = options.Value;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 静默登录
    /// </summary>
    public async Task<WxMaLoginResultDto> SilentLoginAsync(string code, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(code, cancellationToken);
        var openId = session.OpenId;

        if (string.IsNullOrWhiteSpace(openId))
        {
            throw new BusinessException(ResultCode.UserLoginException, "微信登录失败：无法获取用户标识");
        }

        var identity = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUserIdentity>().Where(s => s.Provider == WechatProvider && s.ProviderAccountId == openId),
            cancellationToken);

        if (identity is not null)
        {
            var token = await GenerateTokenByUserIdAsync(identity.UserId, cancellationToken);
            return WxMaLoginResultDto.Success(token);
        }

        _logger.LogInformation("微信小程序静默登录：用户未绑定手机号，openId={OpenId}", openId);
        return WxMaLoginResultDto.RequireBindMobile();
    }

    /// <summary>
    /// 手机号快捷登录
    /// </summary>
    public async Task<AuthenticationTokenDto> PhoneLoginAsync(string loginCode, string phoneCode, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(loginCode, cancellationToken);
        var openId = session.OpenId;

        if (string.IsNullOrWhiteSpace(openId))
        {
            throw new BusinessException(ResultCode.UserLoginException, "微信登录失败：无法获取用户标识");
        }

        var mobile = await GetPhoneNumberAsync(phoneCode, cancellationToken);

        _logger.LogInformation("微信小程序手机号快捷登录：openId={OpenId}, mobile={Mobile}", openId, mobile);

        // 建用户、分配角色与绑定 openid 同一事务，绑定冲突时不留下孤立账号
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var user = await FindOrCreateUserAsync(mobile, cancellationToken);
        await BindWechatOpenIdAsync(user.Id, openId, session.SessionKey, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GenerateTokenByUserIdAsync(user.Id, cancellationToken);
    }

    /// <summary>
    /// 绑定手机号
    /// </summary>
    public async Task<AuthenticationTokenDto> BindMobileAsync(string loginCode, string mobile, string smsCode, CancellationToken cancellationToken = default)
    {
        // openid 只从微信接口取得，不接受客户端提交的身份标识
        var session = await GetSessionAsync(loginCode, cancellationToken);
        var openId = session.OpenId;
        if (string.IsNullOrWhiteSpace(openId))
        {
            throw new BusinessException(ResultCode.UserLoginException, "微信登录失败：无法获取用户标识");
        }

        await ValidateSmsCodeAsync(mobile, smsCode, cancellationToken);

        // 建用户、分配角色与绑定 openid 同一事务，绑定冲突时不留下孤立账号
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var user = await FindOrCreateUserAsync(mobile, cancellationToken);
        await BindWechatOpenIdAsync(user.Id, openId, session.SessionKey, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("微信小程序绑定手机号成功：mobile={Mobile}, openId={OpenId}", mobile, openId);

        return await GenerateTokenByUserIdAsync(user.Id, cancellationToken);
    }

    private async Task<WechatSessionResponse> GetSessionAsync(string code, CancellationToken cancellationToken)
    {
        var url = string.Format(JsCode2SessionUrl, _options.AppId, _options.AppSecret, code);
        var httpClient = _httpClientFactory.CreateClient("Wechat");

        try
        {
            var response = await httpClient.GetFromJsonAsync<WechatSessionResponse>(url, JsonOptions, cancellationToken);
            if (response is null || response.ErrCode is not null and not 0)
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

    /// <summary>
    /// 用 phoneCode 调微信接口换取用户手机号
    /// </summary>
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

    /// <summary>
    /// 取 access_token：优先读 Redis 缓存，未命中则调微信并缓存（提前 5 分钟过期）
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(RedisKeyConstants.Wechat.AccessToken, _options.AppId);

        // 先从缓存获取
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        // 请求 access_token
        var url = string.Format(GetAccessTokenUrl, _options.AppId, _options.AppSecret);
        var httpClient = _httpClientFactory.CreateClient("Wechat");

        var response = await httpClient.GetFromJsonAsync<WechatAccessTokenResponse>(url, JsonOptions, cancellationToken);
        if (response is null || response.ErrCode is not null and not 0)
        {
            var errMsg = response?.ErrMsg ?? "Unknown error";
            throw new BusinessException(ResultCode.UserLoginException, $"获取微信AccessToken失败：{errMsg}");
        }

        // 缓存 token（提前 5 分钟过期）
        var expiresIn = Math.Max(response.ExpiresIn - 300, 60);
        await _cache.SetStringAsync(cacheKey, response.AccessToken,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expiresIn) },
            cancellationToken);

        return response.AccessToken;
    }

    /// <summary>
    /// 按手机号查用户，不存在则新建并分配默认角色
    /// </summary>
    private async Task<SysUser> FindOrCreateUserAsync(string mobile, CancellationToken cancellationToken)
    {
        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>().Where(u => u.Mobile == mobile && !u.IsDeleted),
            cancellationToken);

        if (user is not null)
        {
            return user;
        }

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

        _userRepo.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 分配默认角色
        if (_options.DefaultRoleId > 0)
        {
            _userRoleRepo.Add(new SysUserRole
            {
                UserId = user.Id,
                RoleId = _options.DefaultRoleId
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("微信小程序登录：创建新用户，mobile={Mobile}, userId={UserId}", mobile, user.Id);

        return user;
    }

    /// <summary>
    /// 绑定微信 openid 并缓存会话密钥；openid 已属于其他用户时拒绝绑定
    /// </summary>
    private async Task BindWechatOpenIdAsync(long userId, string openId, string? sessionKey, CancellationToken cancellationToken)
    {
        var existing = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUserIdentity>().Where(s => s.Provider == WechatProvider && s.ProviderAccountId == openId),
            cancellationToken);

        if (existing is not null && existing.UserId != userId)
        {
            throw new BusinessException(ResultCode.UserLoginException, "该微信账号已关联其他用户");
        }

        if (existing is null)
        {
            _userIdentityRepo.Add(new SysUserIdentity
            {
                UserId = userId,
                Provider = WechatProvider,
                ProviderAccountId = openId,
                LastLoginTime = DateTime.Now,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            });
        }
        else
        {
            existing.LastLoginTime = DateTime.Now;
            existing.UpdateTime = DateTime.Now;
            _userIdentityRepo.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(sessionKey))
        {
            var cacheKey = string.Format(RedisKeyConstants.Wechat.SessionKey, WechatProvider, openId);
            await _cache.SetStringAsync(cacheKey, sessionKey, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3)
            }, cancellationToken);
        }
    }

    /// <summary>
    /// 校验短信验证码，正确则删除缓存防止重放
    /// </summary>
    private async Task ValidateSmsCodeAsync(string mobile, string smsCode, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RedisKeyConstants.Captcha.MobileCode, mobile.Trim());
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrEmpty(cached))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeExpired);
        }

        if (!string.Equals(cached, smsCode, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.UserVerificationCodeError);
        }

        // 验证成功后删除验证码
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    /// <summary>
    /// 按用户角色与数据权限范围组装载荷并签发令牌
    /// </summary>
    private async Task<AuthenticationTokenDto> GenerateTokenByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>().Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => new { u.Id, u.Username, u.DeptId }),
            cancellationToken);

        if (user is null)
        {
            throw new BusinessException(ResultCode.UserLoginException, "用户不存在");
        }

        // 取用户启用的角色编码与各自数据权限范围
        var rolesQuery =
            from ur in _readDb.Query<SysUserRole>()
            join r in _readDb.Query<SysRole>() on ur.RoleId equals r.Id
            where ur.UserId == user.Id && !r.IsDeleted && r.Status == 1
            select new { r.Id, r.Code, r.DataScope };

        var roles = await _readDb.ToListAsync(rolesQuery, cancellationToken);

        // 角色编码加 ROLE_ 前缀作为权限点
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
                var customDeptIds = await _readDb.ToListAsync(
                    _readDb.Query<SysRoleDept>().Where(rd => rd.RoleId == role.Id).Select(rd => rd.DeptId),
                    cancellationToken);

                roleDataScope.CustomDeptIds = customDeptIds;
            }

            dataScopes.Add(roleDataScope);
        }

        var subject = new AuthTokenSubject(
            UserId: user.Id,
            DeptId: user.DeptId ?? 0,
            DataScopes: dataScopes,
            Username: user.Username ?? string.Empty,
            Authorities: authorities
        );

        return _tokenManager.GenerateToken(subject);
    }

    /// <summary>
    /// 微信小程序 code2session 响应
    /// </summary>
    private sealed class WechatSessionResponse
    {
        /// <summary>微信用户唯一标识 openid</summary>
        [JsonPropertyName("openid")]
        public string? OpenId { get; set; }

        /// <summary>会话密钥 session_key</summary>
        [JsonPropertyName("session_key")]
        public string? SessionKey { get; set; }

        /// <summary>跨应用统一标识 unionid</summary>
        [JsonPropertyName("unionid")]
        public string? UnionId { get; set; }

        /// <summary>错误码，0 或缺失表示成功</summary>
        [JsonPropertyName("errcode")]
        public int? ErrCode { get; set; }

        /// <summary>错误信息</summary>
        [JsonPropertyName("errmsg")]
        public string? ErrMsg { get; set; }
    }

    /// <summary>
    /// 微信小程序 access_token 响应
    /// </summary>
    private sealed class WechatAccessTokenResponse
    {
        /// <summary>接口调用凭据</summary>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>凭据有效期（秒）</summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>错误码，0 或缺失表示成功</summary>
        [JsonPropertyName("errcode")]
        public int? ErrCode { get; set; }

        /// <summary>错误信息</summary>
        [JsonPropertyName("errmsg")]
        public string? ErrMsg { get; set; }
    }

    /// <summary>
    /// 微信小程序手机号解密响应
    /// </summary>
    private sealed class WechatPhoneNumberResponse
    {
        /// <summary>错误码，0 表示成功</summary>
        [JsonPropertyName("errcode")]
        public int ErrCode { get; set; }

        /// <summary>错误信息</summary>
        [JsonPropertyName("errmsg")]
        public string ErrMsg { get; set; } = string.Empty;

        /// <summary>解密后的手机号信息</summary>
        [JsonPropertyName("phone_info")]
        public WechatPhoneInfo? PhoneInfo { get; set; }
    }

    /// <summary>
    /// 微信小程序解密后的手机号信息
    /// </summary>
    private sealed class WechatPhoneInfo
    {
        /// <summary>完整手机号</summary>
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        /// <summary>不含区号的手机号</summary>
        [JsonPropertyName("purePhoneNumber")]
        public string? PurePhoneNumber { get; set; }

        /// <summary>区号（如 86）</summary>
        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }
    }
}
