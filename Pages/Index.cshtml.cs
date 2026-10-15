using System.Security.Cryptography;
using System.Text;
using BestInTown.Models;
using BestInTown.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BestInTown.Pages;

public class IndexModel(VotingService svc) : PageModel
{
    public Topic Topic { get; set; } = null!;
    public List<LeaderboardRow> Rows { get; set; } = new();

    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string? Neighborhood { get; set; }

    public async Task OnGetAsync()
    {
        Topic = await svc.GetCurrentTopicAsync();
        Rows = await svc.LeaderboardAsync(Topic.Id);
    }

    public async Task<IActionResult> OnPostVoteAsync(int nominationId)
    {
        var (voterKey, ipHash) = Identify();
        TempData["Msg"] = await svc.VoteAsync(nominationId, voterKey, ipHash) ?? "Гласът ви е приет!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostNominateAsync()
    {
        var topic = await svc.GetCurrentTopicAsync();
        TempData["Msg"] = await svc.NominateAsync(topic.Id, Name, Neighborhood) ?? "Мястото е номинирано!";
        return RedirectToPage();
    }

    // Anonymous voter id in a cookie + salted hash of the IP (no raw IPs stored).
    private (string voterKey, string ipHash) Identify()
    {
        if (!Request.Cookies.TryGetValue("voter", out var id) || !Guid.TryParse(id, out _))
        {
            id = Guid.NewGuid().ToString();
            Response.Cookies.Append("voter", id, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        }
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("change-this-salt:" + ip)));
        return (id!, hash);
    }
}
