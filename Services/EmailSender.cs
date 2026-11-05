using System.Net;
using System.Net.Mail;

namespace BestInTown.Services;

// Sends mail through any SMTP server (Gmail, abv.bg, SendGrid, ...). Settings: section "Smtp".
// With no Smtp:Host set (local development), the message is written to the log instead of sent.
public class EmailSender(IConfiguration cfg, ILogger<EmailSender> log)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(cfg["Smtp:Host"]);

    public async Task SendAsync(string to, string subject, string body)
    {
        var s = cfg.GetSection("Smtp");
        var host = s["Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            log.LogWarning("Smtp:Host is not set, email NOT sent. To: {To} | {Subject}\n{Body}", to, subject, body);
            return;
        }

        var from = new[] { s["From"], s["User"] }.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                   ?? throw new InvalidOperationException("Set Smtp:From (or Smtp:User).");
        var fromName = string.IsNullOrWhiteSpace(s["FromName"]) ? "Най-доброто в града" : s["FromName"];
        using var msg = new MailMessage(new MailAddress(from, fromName), new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        using var client = new SmtpClient(host, int.TryParse(s["Port"], out var port) ? port : 587)
        {
            EnableSsl = !bool.TryParse(s["EnableSsl"], out var ssl) || ssl,
            Credentials = string.IsNullOrEmpty(s["User"]) ? null : new NetworkCredential(s["User"], s["Password"]),
            Timeout = 15000,
        };
        await client.SendMailAsync(msg);
    }
}
