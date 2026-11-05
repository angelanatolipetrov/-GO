using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DnsClient;

namespace BestInTown.Services;

public record EmailCheck(string? ErrorKey, string? Suggestion = null)
{
    public static readonly EmailCheck Valid = new(ErrorKey: null);
    public bool Ok => ErrorKey == null;
}

// Checks that an address can actually receive mail: valid format, not a throwaway inbox,
// and the domain exists with a mail server (MX record). It cannot prove the mailbox itself exists –
// only a confirmation email can do that.
public class EmailChecker(ILookupClient dns, ILogger<EmailChecker> log)
{
    private static readonly HashSet<string> Disposable = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "10minutemail.com", "10minutemail.net", "guerrillamail.com", "guerrillamail.net",
        "sharklasers.com", "grr.la", "yopmail.com", "yopmail.net", "temp-mail.org", "temp-mail.io",
        "tempmail.com", "tempmail.net", "tempail.com", "trashmail.com", "trashmail.de", "getnada.com",
        "dispostable.com", "maildrop.cc", "throwawaymail.com", "fakeinbox.com", "mohmal.com",
        "emailondeck.com", "mintemail.com", "1secmail.com", "1secmail.org", "mailnesia.com",
        "burnermail.io", "spamgourmet.com", "moakt.com", "tempr.email", "discard.email",
        "mailcatch.com", "spambox.us", "trbvm.com", "emltmp.com", "tmpmail.org", "tmpmail.net",
    };

    // Popular providers in Bulgaria and worldwide, used to catch typos like "gmial.com".
    private static readonly string[] Popular =
    {
        "gmail.com", "abv.bg", "mail.bg", "dir.bg", "yahoo.com", "hotmail.com", "outlook.com",
        "live.com", "icloud.com", "proton.me", "protonmail.com", "gbg.bg",
    };

    // Real providers that are one or two letters away from a popular one – never "correct" these.
    private static readonly HashSet<string> KnownGood = new(StringComparer.OrdinalIgnoreCase)
    {
        "mail.com", "email.com", "ymail.com", "gmx.com", "gmx.net", "gmx.de", "aol.com", "mail.ru",
        "yandex.ru", "yahoo.co.uk", "hotmail.de", "hotmail.fr", "hotmail.it", "live.de", "live.fr",
        "outlook.de", "outlook.fr", "me.com", "mac.com", "inbox.ru", "bk.ru", "list.ru", "gmail.bg",
    };

    // skipTypo: the user already saw the "did you mean" hint and kept their address.
    public async Task<EmailCheck> CheckAsync(string email, bool skipTypo = false, CancellationToken ct = default)
    {
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            return new("err.email");

        var domain = parsed.Host.ToLowerInvariant();
        // Plain domain with a real-looking ending: "abv.bg", "mail.co.uk", not "a@b", "x@y.1" or "x@[1.2.3.4]".
        if (!Regex.IsMatch(domain, @"^([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+([a-z]{2,24}|xn--[a-z0-9-]{2,59})$")
            || !Regex.IsMatch(parsed.User, @"^[A-Za-z0-9._%+'-]{1,64}$") || parsed.User.StartsWith('.') || parsed.User.Contains(".."))
            return new("err.email");

        if (Disposable.Contains(domain)) return new("err.email.disposable");

        if (!skipTypo && !KnownGood.Contains(domain) && !Popular.Contains(domain))
        {
            var typo = Popular.FirstOrDefault(p => Distance(p, domain) <= 2);
            if (typo != null) return new("err.email.typo", parsed.User + "@" + typo);
        }

        // Hard limit for the whole lookup, so a slow DNS server never hangs the sign-up page.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        ct = timeout.Token;

        try
        {
            var mx = await dns.QueryAsync(domain, QueryType.MX, cancellationToken: ct);
            if (mx.Header.ResponseCode == DnsHeaderResponseCode.NotExistentDomain) return Log(domain, "no such domain", new("err.email.domain"));
            if (!mx.HasError)
            {
                var servers = mx.Answers.MxRecords().ToList();
                // "Null MX" (RFC 7505): the domain says it accepts no mail.
                if (servers.Count == 1 && servers[0].Exchange.Value is "." or "") return Log(domain, "null MX", new("err.email.domain"));
                if (servers.Count > 0) return Log(domain, "has MX", EmailCheck.Valid);

                // No MX: mail falls back to the domain's own address (RFC 5321), so an A record is enough.
                var a = await dns.QueryAsync(domain, QueryType.A, cancellationToken: ct);
                if (!a.HasError) return a.Answers.ARecords().Any()
                    ? Log(domain, "no MX, has A", EmailCheck.Valid)
                    : Log(domain, "no mail server", new("err.email.domain"));
            }
            log.LogWarning("DNS error {Code} for {Domain}, using system lookup", mx.Header.ResponseCode, domain);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "DNS lookup failed for {Domain}, using system lookup", domain);
        }

        // Fallback: the operating system's own resolver. It can at least tell a domain that doesn't exist.
        try
        {
            var addrs = await Dns.GetHostAddressesAsync(domain, ct);
            return addrs.Length > 0 ? Log(domain, "system lookup ok", EmailCheck.Valid) : Log(domain, "system lookup empty", new("err.email.domain"));
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.HostNotFound or SocketError.NoData)
        {
            return Log(domain, "system lookup: not found", new("err.email.domain"));
        }
        catch (Exception ex)
        {
            // No DNS at all (offline?): don't block real people because of our network problem.
            log.LogWarning(ex, "System lookup failed for {Domain}; accepting", domain);
            return EmailCheck.Valid;
        }
    }

    private EmailCheck Log(string domain, string reason, EmailCheck result)
    {
        log.LogInformation("Email domain {Domain}: {Reason} -> {Result}", domain, reason, result.Ok ? "ok" : result.ErrorKey);
        return result;
    }

    private static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);   // swapped letters: gmial -> gmail
        }
        return d[a.Length, b.Length];
    }
}
