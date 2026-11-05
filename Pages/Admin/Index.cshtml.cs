using BestInTown.Models;
using BestInTown.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BestInTown.Pages.Admin;

public class IndexModel(VotingService svc, PhotoStore photos) : PageModel
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

    public async Task<IActionResult> OnPostCoverAsync(int id, IFormFile? cover)
    {
        var error = await photos.SaveTopicAsync(id, cover);
        if (error != null) TempData["Msg"] = error;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCardAsync(int id, IFormFile? cover)
    {
        var error = await photos.SaveCardAsync(id, cover);
        if (error != null) TempData["Msg"] = error;
        return RedirectToPage();
    }

    public IActionResult OnPostDeleteCard(int id)
    {
        photos.DeleteCard(id);
        return RedirectToPage();
    }

    public IActionResult OnPostDeleteCover(int id)
    {
        photos.DeleteTopic(id);
        return RedirectToPage();
    }

    public IActionResult OnPostDeletePhoto(int placeId)
    {
        photos.Delete(placeId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRenameAsync(int id, string? title, string? titleEn)
    {
        await svc.RenameTopicAsync(id, title, titleEn);
        return RedirectToPage();
    }
}
