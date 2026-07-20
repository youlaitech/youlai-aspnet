namespace Youlai.Application.Results;

/// <summary>
/// 业务结果码枚举
/// </summary>
public enum ResultCode
{
    /// <summary>成功</summary>
    Success,

    // 用户端错误
    /// <summary>用户端错误</summary>
    UserError,
    /// <summary>用户注册错误</summary>
    UserRegistrationError,
    /// <summary>用户未同意隐私协议</summary>
    UserNotAgreePrivacyAgreement,
    /// <summary>校验码输入错误</summary>
    VerificationCodeInputError,
    /// <summary>用户登录异常</summary>
    UserLoginException,
    /// <summary>用户账户不存在</summary>
    AccountNotFound,
    /// <summary>用户账户被冻结</summary>
    AccountFrozen,
    /// <summary>用户名或密码错误</summary>
    UserPasswordError,
    /// <summary>访问令牌无效或已过期</summary>
    AccessTokenInvalid,
    /// <summary>刷新令牌无效或已过期</summary>
    RefreshTokenInvalid,
    /// <summary>验证码错误</summary>
    UserVerificationCodeError,
    /// <summary>用户验证码尝试次数超限</summary>
    UserVerificationCodeAttemptLimitExceeded,
    /// <summary>用户验证码过期</summary>
    UserVerificationCodeExpired,
    /// <summary>访问权限异常</summary>
    AccessPermissionException,
    /// <summary>访问未授权</summary>
    AccessUnauthorized,
    /// <summary>用户请求参数错误</summary>
    UserRequestParameterError,
    /// <summary>无效的用户输入</summary>
    InvalidUserInput,
    /// <summary>请求必填参数为空</summary>
    RequestRequiredParameterIsEmpty,
    /// <summary>参数格式不匹配</summary>
    ParameterFormatMismatch,
    /// <summary>用户请求服务异常</summary>
    UserRequestServiceException,
    /// <summary>请求并发数超出限制</summary>
    RequestConcurrencyLimitExceeded,
    /// <summary>请勿重复提交</summary>
    DuplicateSubmission,
    /// <summary>上传文件异常</summary>
    UploadFileException,
    /// <summary>删除文件异常</summary>
    DeleteFileException,
    /// <summary>扫码登录票据不存在或已过期</summary>
    QrCodeNotFound,
    /// <summary>扫码登录状态非法</summary>
    QrCodeStatusIllegal,
    /// <summary>扫码用户与确认用户不一致</summary>
    QrCodeUserMismatch,
    /// <summary>扫码登录票据已使用</summary>
    QrCodeAlreadyUsed,

    // 系统执行出错
    /// <summary>系统执行出错</summary>
    SystemError,
    /// <summary>系统执行超时</summary>
    SystemExecutionTimeout,

    // 调用第三方服务出错
    /// <summary>调用第三方服务出错</summary>
    ThirdPartyServiceError,
    /// <summary>接口不存在</summary>
    InterfaceNotExist,
    /// <summary>数据库服务出错</summary>
    DatabaseServiceError,
    /// <summary>数据库执行异常</summary>
    DatabaseExecutionError,
    /// <summary>数据库执行语法错误</summary>
    DatabaseExecutionSyntaxError,
    /// <summary>违反了完整性约束</summary>
    IntegrityConstraintViolation,
    /// <summary>数据库拒绝访问</summary>
    DatabaseAccessDenied,
}

/// <summary>
/// 结果码扩展：消息映射与 Result 构造
/// </summary>
public static class ResultCodeExtensions
{
    /// <summary>取结果码对应的 5 位业务码字符串</summary>
    public static string GetCode(this ResultCode code)
    {
        return code switch
        {
            ResultCode.Success => "00000",

            // 用户端错误
            ResultCode.UserError => "A0001",
            ResultCode.UserRegistrationError => "A0100",
            ResultCode.UserNotAgreePrivacyAgreement => "A0101",
            ResultCode.VerificationCodeInputError => "A0130",
            ResultCode.UserLoginException => "A0200",
            ResultCode.AccountNotFound => "A0201",
            ResultCode.AccountFrozen => "A0202",
            ResultCode.UserPasswordError => "A0210",
            ResultCode.AccessTokenInvalid => "A0230",
            ResultCode.RefreshTokenInvalid => "A0231",
            ResultCode.UserVerificationCodeError => "A0240",
            ResultCode.UserVerificationCodeAttemptLimitExceeded => "A0241",
            ResultCode.UserVerificationCodeExpired => "A0242",
            ResultCode.QrCodeNotFound => "A0250",
            ResultCode.QrCodeStatusIllegal => "A0251",
            ResultCode.QrCodeUserMismatch => "A0252",
            ResultCode.QrCodeAlreadyUsed => "A0253",
            ResultCode.AccessPermissionException => "A0300",
            ResultCode.AccessUnauthorized => "A0301",
            ResultCode.UserRequestParameterError => "A0400",
            ResultCode.InvalidUserInput => "A0402",
            ResultCode.RequestRequiredParameterIsEmpty => "A0410",
            ResultCode.ParameterFormatMismatch => "A0421",
            ResultCode.UserRequestServiceException => "A0500",
            ResultCode.RequestConcurrencyLimitExceeded => "A0502",
            ResultCode.DuplicateSubmission => "A0506",
            ResultCode.UploadFileException => "A0700",
            ResultCode.DeleteFileException => "A0710",

            // 系统执行出错
            ResultCode.SystemError => "B0001",
            ResultCode.SystemExecutionTimeout => "B0100",

            // 调用第三方服务出错
            ResultCode.ThirdPartyServiceError => "C0001",
            ResultCode.InterfaceNotExist => "C0113",
            ResultCode.DatabaseServiceError => "C0300",
            ResultCode.DatabaseExecutionError => "C0310",
            ResultCode.DatabaseExecutionSyntaxError => "C0313",
            ResultCode.IntegrityConstraintViolation => "C0342",
            ResultCode.DatabaseAccessDenied => "C0351",

            _ => "B0001",
        };
    }

    /// <summary>取结果码对应的默认提示文案</summary>
    public static string GetMsg(this ResultCode code)
    {
        return code switch
        {
            ResultCode.Success => "成功",

            // 用户端错误
            ResultCode.UserError => "用户端错误",
            ResultCode.UserRegistrationError => "用户注册错误",
            ResultCode.UserNotAgreePrivacyAgreement => "用户未同意隐私协议",
            ResultCode.VerificationCodeInputError => "校验码输入错误",
            ResultCode.UserLoginException => "用户登录异常",
            ResultCode.AccountNotFound => "用户账户不存在",
            ResultCode.AccountFrozen => "用户账户被冻结",
            ResultCode.UserPasswordError => "用户名或密码错误",
            ResultCode.AccessTokenInvalid => "访问令牌无效或已过期",
            ResultCode.RefreshTokenInvalid => "刷新令牌无效或已过期",
            ResultCode.UserVerificationCodeError => "验证码错误",
            ResultCode.UserVerificationCodeAttemptLimitExceeded => "用户验证码尝试次数超限",
            ResultCode.UserVerificationCodeExpired => "用户验证码过期",
            ResultCode.QrCodeNotFound => "扫码登录票据不存在或已过期",
            ResultCode.QrCodeStatusIllegal => "扫码登录状态非法",
            ResultCode.QrCodeUserMismatch => "扫码用户与确认用户不一致",
            ResultCode.QrCodeAlreadyUsed => "扫码登录票据已使用",
            ResultCode.AccessPermissionException => "访问权限异常",
            ResultCode.AccessUnauthorized => "访问未授权",
            ResultCode.UserRequestParameterError => "用户请求参数错误",
            ResultCode.InvalidUserInput => "无效的用户输入",
            ResultCode.RequestRequiredParameterIsEmpty => "请求必填参数为空",
            ResultCode.ParameterFormatMismatch => "参数格式不匹配",
            ResultCode.UserRequestServiceException => "用户请求服务异常",
            ResultCode.RequestConcurrencyLimitExceeded => "请求并发数超出限制",
            ResultCode.DuplicateSubmission => "请勿重复提交",
            ResultCode.UploadFileException => "上传文件异常",
            ResultCode.DeleteFileException => "删除文件异常",

            // 系统执行出错
            ResultCode.SystemError => "系统执行出错",
            ResultCode.SystemExecutionTimeout => "系统执行超时",

            // 调用第三方服务出错
            ResultCode.ThirdPartyServiceError => "调用第三方服务出错",
            ResultCode.InterfaceNotExist => "接口不存在",
            ResultCode.DatabaseServiceError => "数据库服务出错",
            ResultCode.DatabaseExecutionError => "数据库执行异常",
            ResultCode.DatabaseExecutionSyntaxError => "数据库执行语法错误",
            ResultCode.IntegrityConstraintViolation => "违反了完整性约束",
            ResultCode.DatabaseAccessDenied => "数据库拒绝访问",

            _ => "系统执行出错",
        };
    }

    /// <summary>取业务码（GetCode 简写）</summary>
    public static string Code(this ResultCode code) => code.GetCode();
    /// <summary>取提示文案（GetMsg 简写）</summary>
    public static string Msg(this ResultCode code) => code.GetMsg();

    /// <summary>取结果码对应的 HTTP 状态码（401/403/404/200 等）</summary>
    public static int GetHttpStatus(this ResultCode code) => code switch
    {
        // 未认证
        ResultCode.AccessTokenInvalid or ResultCode.RefreshTokenInvalid or ResultCode.AccessUnauthorized => 401,
        // 权限不足
        ResultCode.AccessPermissionException => 403,
        // 接口不存在
        ResultCode.InterfaceNotExist => 404,
        // 限流
        ResultCode.RequestConcurrencyLimitExceeded => 429,
        // 系统/数据库异常
        ResultCode.SystemError or ResultCode.SystemExecutionTimeout
            or ResultCode.DatabaseServiceError or ResultCode.DatabaseExecutionError
            or ResultCode.DatabaseExecutionSyntaxError
            or ResultCode.IntegrityConstraintViolation or ResultCode.DatabaseAccessDenied => 500,
        // 其他错误默认 400
        _ => 400,
    };
}
