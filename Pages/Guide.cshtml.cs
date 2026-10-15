using BestInTown.Models;
using BestInTown.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BestInTown.Pages;

public class GuideModel(VotingService svc) : PageModel
{
    public List<GuideEntry> Entries { get; set; } = new();
    public async Task OnGetAsync() => Entries = await svc.GuideAsync();
}
