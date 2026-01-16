namespace Youlai.Application.Common.Results;

/// <summary>
/// 业务状态码枚举
/// </summary>
public enum ResultCode
{
    Success = 0,

    UserError = 1,
    UserLoginException = 2,
    UserPasswordError = 3,
    AccessTokenInvalid = 4,
    RefreshTokenInvalid = 5,
    UserVerificationCodeError = 12,
    UserVerificationCodeExpired = 13,
    AccessPermissionException = 6,
    AccessUnauthorized = 7,
    UserRequestParameterError = 8,
    InvalidUserInput = 9,
    RequestRequiredParameterIsEmpty = 10,
    ParameterFormatMismatch = 11,

    UploadFileException = 70,
    DeleteFileException = 71,

    SystemError = 1000,

    ThirdPartyServiceError = 2000,
    InterfaceNotExist = 2001,
    DatabaseServiceError = 2002,
    DatabaseExecutionSyntaxError = 2003,
    IntegrityConstraintViolation = 2004,
    DatabaseAccessDenied = 2005
}

/// <summary>
/// ResultCode 的 code/msg 映射
/// </summary>
public static class ResultCodeExtensions
{
    public static string GetCode(this ResultCode code)
    {
        return code switch
        {
            ResultCode.Success => "00000",

            ResultCode.UserError => "A0001",
            ResultCode.UserLoginException => "A0200",
            ResultCode.UserPasswordError => "A0210",
            ResultCode.AccessTokenInvalid => "A0230",
            ResultCode.RefreshTokenInvalid => "A0231",
            ResultCode.UserVerificationCodeError => "A0240",
            ResultCode.UserVerificationCodeExpired => "A0242",
            ResultCode.AccessPermissionException => "A0300",
            ResultCode.AccessUnauthorized => "A0301",
            ResultCode.UserRequestParameterError => "A0400",
            ResultCode.InvalidUserInput => "A0402",
            ResultCode.RequestRequiredParameterIsEmpty => "A0410",
            ResultCode.ParameterFormatMismatch => "A0421",

            ResultCode.UploadFileException => "A0700",
            ResultCode.DeleteFileException => "A0710",

            ResultCode.SystemError => "B0001",

            ResultCode.ThirdPartyServiceError => "C0001",
            ResultCode.InterfaceNotExist => "C0113",
            ResultCode.DatabaseServiceError => "C0300",
            ResultCode.DatabaseExecutionSyntaxError => "C0313",
            ResultCode.IntegrityConstraintViolation => "C0342",
            ResultCode.DatabaseAccessDenied => "C0351",

            _ => "B0001",
        };
    }

    public static string GetMsg(this ResultCode code)
    {
        return code switch
        {
            ResultCode.Success => "成功",

            ResultCode.UserError => "用户端错误",
            ResultCode.UserLoginException => "用户登录异常",
            ResultCode.UserPasswordError => "用户名或密码错误",
            ResultCode.AccessTokenInvalid => "访问令牌无效或已过期",
            ResultCode.RefreshTokenInvalid => "刷新令牌无效或已过期",
            ResultCode.UserVerificationCodeError => "验证码错误",
            ResultCode.UserVerificationCodeExpired => "用户验证码过期",
            ResultCode.AccessPermissionException => "访问权限异常",
            ResultCode.AccessUnauthorized => "访问未授权",
            ResultCode.UserRequestParameterError => "用户请求参数错误",
            ResultCode.InvalidUserInput => "无效的用户输入",
            ResultCode.RequestRequiredParameterIsEmpty => "请求必填参数为空",
            ResultCode.ParameterFormatMismatch => "参数格式不匹配",

            ResultCode.UploadFileException => "上传文件异常",
            ResultCode.DeleteFileException => "删除文件异常",

            ResultCode.SystemError => "系统执行出错",

            ResultCode.ThirdPartyServiceError => "调用第三方服务出错",
            ResultCode.InterfaceNotExist => "接口不存在",
            ResultCode.DatabaseServiceError => "数据库服务出错",
            ResultCode.DatabaseExecutionSyntaxError => "数据库执行语法错误",
            ResultCode.IntegrityConstraintViolation => "违反了完整性约束",
            ResultCode.DatabaseAccessDenied => "演示环境已禁用数据库写入功能，请本地部署修改数据库链接或开启Mock模式进行体验",

            _ => "系统执行出错",
        };
    }

    public static string Code(this ResultCode code) => code.GetCode();

    public static string Msg(this ResultCode code) => code.GetMsg();
}
