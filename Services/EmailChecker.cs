using System.Net.Mail;
using DnsClient;

namespace BestInTown.Services;

public record EmailCheck(string? ErrorKey, string? Suggestion = null)
{
    public static readonly EmailCheck Valid = EmailCheck.Valid;
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
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || !parsed.Host.Contains('.'))
            return new("err.email");

        var domain = parsed.Host.ToLowerInvariant().TrimEnd('.');
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
            if (mx.Header.ResponseCode == DnsHeaderResponseCode.NotExistentDomain) return new("err.email.domain");
            if (mx.HasError) return EmailCheck.Valid;   // server failure etc. – don't guess

            var servers = mx.Answers.MxRecords().ToList();
            // "Null MX" (RFC 7505): the domain says it accepts no mail.
            if (servers.Count == 1 && servers[0].Exchange.Value is "." or "") return new("err.email.domain");
            if (servers.Count > 0) return EmailCheck.Valid;

            // No MX: mail falls back to the domain's own address (RFC 5321), so an A/AAAA record is enough.
            var a = await dns.QueryAsync(domain, QueryType.A, cancellationToken: ct);
            if (a.Answers.ARecords().Any()) return EmailCheck.Valid;
            var aaaa = await dns.QueryAsync(domain, QueryType.AAAA, cancellationToken: ct);
            return aaaa.Answers.AaaaRecords().Any() || a.HasError || aaaa.HasError ? EmailCheck.Valid : new("err.email.domain");
        }
        catch (Exception ex)
        {
            // DNS is down, slow or unreachable (DnsClient reports timeouts as cancellations):
            // don't block real people because of our network problem.
            log.LogWarning(ex, "MX lookup failed for {Domain}", domain);
            return EmailCheck.Valid;
        }
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
