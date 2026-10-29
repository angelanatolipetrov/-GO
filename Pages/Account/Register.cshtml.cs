using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BestInTown.Services;

namespace BestInTown.Pages.Account;

public class RegisterModel(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, Loc L) : PageModel
{
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Password { get; set; }
    [BindProperty] public string? ConfirmPassword { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public List<string> Errors { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = Email?.Trim() ?? "";
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email) Errors.Add(L["err.email"]);
        if (Password != ConfirmPassword) Errors.Add(L["err.pw.mismatch"]);
        if (Errors.Count > 0) return Page();

        var user = new IdentityUser { UserName = email, Email = email };
        var result = await users.CreateAsync(user, Password ?? "");
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
            {
                var key = "idErr." + e.Code;
                var text = L.Has(key) ? L[key] : L["err.generic"];
                if (!Errors.Contains(text)) Errors.Add(text);
            }
            return Page();
        }

        await signIn.SignInAsync(user, isPersistent: true);
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
    }
}
