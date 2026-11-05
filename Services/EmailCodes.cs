using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace BestInTown.Services;

public enum CodeResult { Ok, Wrong, Expired, LockedOut }

// 6-digit email verification codes. Only a hash is stored (in AspNetUserTokens), never the code itself.
// Wrong guesses count towards Identity's lockout (5 tries, then 10 minutes), so codes can't be brute-forced.
public class EmailCodes(UserManager<IdentityUser> users, EmailSender mail, Loc L)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendAfter = TimeSpan.FromSeconds(60);
    private const string Provider = "BestInTown", Name = "EmailCode";

    // Returns false when a code was sent less than a minute ago (nothing is sent then).
    public async Task<bool> SendAsync(IdentityUser user)
    {
        var saved = Parse(await users.GetAuthenticationTokenAsync(user, Provider, Name));
        if (saved != null && DateTime.UtcNow - saved.Value.SentAt < ResendAfter) return false;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var now = DateTime.UtcNow;
        await users.SetAuthenticationTokenAsync(user, Provider, Name,
            $"{Hash(user, code)}|{(now + Lifetime).Ticks}|{now.Ticks}");

        await mail.SendAsync(user.Email!, L.Format("verify.mail.subject", code),
            L.Format("verify.mail.body", code, (int)Lifetime.TotalMinutes));
        return true;
    }

    public async Task<CodeResult> CheckAsync(IdentityUser user, string? code)
    {
        if (await users.IsLockedOutAsync(user)) return CodeResult.LockedOut;

        var saved = Parse(await users.GetAuthenticationTokenAsync(user, Provider, Name));
        if (saved == null || DateTime.UtcNow > saved.Value.ExpiresAt) return CodeResult.Expired;

        code = new string((code ?? "").Where(char.IsDigit).ToArray());   // allow "123 456"
        var ok = code.Length == 6 && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(user, code)), Encoding.ASCII.GetBytes(saved.Value.Hash));

        if (!ok)
        {
            await users.AccessFailedAsync(user);
            return await users.IsLockedOutAsync(user) ? CodeResult.LockedOut : CodeResult.Wrong;
        }

        await users.RemoveAuthenticationTokenAsync(user, Provider, Name);
        await users.ResetAccessFailedCountAsync(user);
        user.EmailConfirmed = true;
        await users.UpdateAsync(user);
        return CodeResult.Ok;
    }

    // Tied to the user id, so a hash copied from one account is useless for another.
    private static string Hash(IdentityUser user, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.Id + ":" + code)));

    private static (string Hash, DateTime ExpiresAt, DateTime SentAt)? Parse(string? value)
    {
        var p = value?.Split('|');
        if (p is not { Length: 3 }
            || !long.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out var exp)
            || !long.TryParse(p[2], NumberStyles.None, CultureInfo.InvariantCulture, out var sent))
            return null;
        return (p[0], new DateTime(exp, DateTimeKind.Utc), new DateTime(sent, DateTimeKind.Utc));
    }
}
