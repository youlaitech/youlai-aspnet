namespace Youlai.Infrastructure.Constants;

/// <summary>
/// Development environment defaults
/// TODO: Replace with real SMS/Email gateway in production
/// </summary>
internal static class DevDefaults
{
    /// <summary>
    /// Default verification code for development environment
    /// </summary>
    public const string VerifyCode = "1234";

    /// <summary>
    /// Default password for new users
    /// </summary>
    public const string DefaultPassword = "123456";
}
