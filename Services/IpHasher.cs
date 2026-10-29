using System.Security.Cryptography;
using System.Text;

namespace BestInTown.Services;

public static class IpHasher
{
    // Salted hash of the client IP; raw IPs are never stored.
    public static string Hash(HttpContext ctx, string salt)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(salt + ":" + ip)));
    }
}
