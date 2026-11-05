using System.Security.Claims;
using BestInTown.Data;
using BestInTown.Models;
using BestInTown.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BestInTown.Pages;

public class IndexModel(VotingService svc, PhotoStore photos, IConfiguration cfg, Loc L) : PageModel
{
    public const string OtherChoice = "__other";

    public Topic Topic { get; set; } = null!;
    public List<LeaderboardRow> Rows { get; set; } = new();
    public List<string> Neighborhoods { get; set; } = new();
    public string[] AllowedNeighborhoods { get; set; } = Array.Empty<string>();
    public HashSet<string> Nominated { get; set; } = new();   // upper-cased names already nominated this week
    public int Total { get; set; }
    public int? MyVote { get; set; }
    public GuideEntry? LastWinner { get; set; }
    public string? CoverUrl { get; set; }   // topic cover from /Admin, else the leader's photo

    [BindProperty(SupportsGet = true, Name = "hood")] public string? Hood { get; set; }
    [BindProperty] public string? Choice { get; set; }         // dropdown pick (catalog place name or "__other")
    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string? Neighborhood { get; set; }
    [BindProperty] public IFormFile? Photo { get; set; }

    public string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string Salt => cfg["Security:IpSalt"] ?? "change-me";

    public async Task OnGetAsync()
    {
        Topic = await svc.GetCurrentTopicAsync();
        AllowedNeighborhoods = cfg.GetSection("Neighborhoods").Get<string[]>() ?? Array.Empty<string>();
        Neighborhoods = await svc.NeighborhoodsAsync(Topic.Id);

        var all = await svc.LeaderboardAsync(Topic.Id);
        Nominated = all.Select(r => r.Name.ToUpperInvariant()).ToHashSet();
        Rows = string.IsNullOrWhiteSpace(Hood) ? all : all.Where(r => r.Neighborhood == Hood).ToList();
        Total = Rows.Sum(r => r.Votes);

        MyVote = await svc.MyVoteAsync(Topic.Id, UserId);
        LastWinner = await svc.LastWinnerAsync();
        CoverUrl = photos.TopicUrl(Topic.Id)
                   ?? all.Where(r => r.Votes > 0).Select(r => photos.Url(r.PlaceId)).FirstOrDefault(u => u != null)
                   ?? all.Select(r => photos.Url(r.PlaceId)).FirstOrDefault(u => u != null);
    }

    public async Task<IActionResult> OnPostVoteAsync(int nominationId)
    {
        if (UserId is null) return LoginRedirect();
        var error = await svc.VoteAsync(nominationId, UserId, IpHasher.Hash(HttpContext, Salt));
        TempData["Msg"] = L[error ?? "msg.voted"];
        return RedirectToPage(new { hood = Hood });
    }

    public async Task<IActionResult> OnPostUnvoteAsync()
    {
        if (UserId is null) return LoginRedirect();
        var topic = await svc.GetCurrentTopicAsync();
        var error = await svc.UnvoteAsync(topic.Id, UserId);
        TempData["Msg"] = L[error ?? "msg.unvoted"];
        return RedirectToPage(new { hood = Hood });
    }

    public async Task<IActionResult> OnPostNominateAsync()
    {
        if (UserId is null) return LoginRedirect();

        string? name = Name, hood = Neighborhood;
        var picked = PlaceCatalog.Find(Choice);
        if (picked != null)
        {
            name = picked.Name;               // trusted: comes from the catalog, not from the form
            hood = picked.Neighborhood;
        }
        else
        {
            var allowed = cfg.GetSection("Neighborhoods").Get<string[]>() ?? Array.Empty<string>();
            if (allowed.Length > 0 && !allowed.Contains(hood ?? "")) hood = null;   // only configured neighborhoods
        }

        var topic = await svc.GetCurrentTopicAsync();
        var error = await svc.NominateAsync(topic.Id, name, hood, UserId);
        if (error == null && Photo != null && await svc.FindPlaceIdAsync(name) is int placeId)
        {
            var photoError = await photos.SaveAsync(placeId, Photo);
            if (photoError != null) error = photoError == "err.photo.exists" ? null : "msg.nominated.noPhoto";
        }
        TempData["Msg"] = L[error ?? "msg.nominated"];
        return RedirectToPage(new { hood = Hood });
    }

    private IActionResult LoginRedirect()
    {
        TempData["Msg"] = L["msg.loginRequired"];
        return RedirectToPage("/Account/Login", new { returnUrl = Url.Page("/Index", new { hood = Hood }) });
    }
}
