using BestInTown.Models;
using BestInTown.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BestInTown.Pages.Admin;

public class IndexModel(VotingService svc) : PageModel
{
    public Topic Topic { get; set; } = null!;
    public List<LeaderboardRow> Rows { get; set; } = new();

    public async Task OnGetAsync()
    {
        Topic = await svc.GetCurrentTopicAsync();
        Rows = await svc.LeaderboardAsync(Topic.Id);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await svc.DeleteNominationAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRenameAsync(int id, string? title, string? titleEn)
    {
        await svc.RenameTopicAsync(id, title, titleEn);
        return RedirectToPage();
    }
}
