using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using Youlai.Domain.Entities;

namespace Youlai.Infrastructure.Security;

/// <summary>
/// BCrypt 密码哈希，兼容已有 BCrypt 哈希
/// </summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher<SysUser>
{
    public string HashPassword(SysUser user, string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public PasswordVerificationResult VerifyHashedPassword(SysUser user, string hashedPassword, string providedPassword)
        => BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
}
