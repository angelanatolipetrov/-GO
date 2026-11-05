using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BestInTown.Services;

namespace BestInTown.Pages.Account;

public class RegisterModel(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, EmailChecker emails, Loc L) : PageModel
{
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Password { get; set; }
    [BindProperty] public string? ConfirmPassword { get; set; }
    [BindProperty] public string? TypoWarned { get; set; }   // address we already showed a "did you mean" hint for
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public List<string> Errors { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = Email?.Trim() ?? "";
        if (Password != ConfirmPassword) Errors.Add(L["err.pw.mismatch"]);

        var keptAfterHint = string.Equals(TypoWarned, email, StringComparison.OrdinalIgnoreCase);
        var check = await emails.CheckAsync(email, keptAfterHint, HttpContext.RequestAborted);
        if (check.Suggestion != null)
        {
            Errors.Add(L.Format(check.ErrorKey!, check.Suggestion, email));
            TypoWarned = email;
            Email = check.Suggestion;   // prefill the corrected address
            ModelState.Remove(nameof(Email));
            ModelState.Remove(nameof(TypoWarned));
        }
        else if (!check.Ok) Errors.Add(L[check.ErrorKey!]);
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
