using System.Net;
using System.Net.Sockets;

namespace Youlai.Api.Security;

/// <summary>
/// 客户端 IP 解析：直连用远端地址，远端是回环或内网代理时才信任 X-Forwarded-For
/// </summary>
public static class ClientIp
{
    public static string Resolve(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is not null && !IsPrivateAddress(remoteIp))
        {
            return remoteIp.ToString();
        }

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return remoteIp?.ToString() ?? "unknown";
    }

    private static bool IsPrivateAddress(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (ip.AddressFamily != AddressFamily.InterNetwork)
        {
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal;
        }

        var bytes = ip.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }
}
