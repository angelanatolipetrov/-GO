using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BestInTown.Services;

namespace BestInTown.Pages.Account;

public class RegisterModel(UserManager<IdentityUser> users, EmailChecker emails, EmailCodes codes,
                           ILogger<RegisterModel> log, Loc L) : PageModel
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

        // Not signed in yet: the account works only after the emailed code is entered.
        try
        {
            await codes.SendAsync(user);
            TempData["Msg"] = L["verify.sent"];
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Could not send verification code to {Email}", email);
            TempData["Msg"] = L["verify.sendFailed"];
        }
        return RedirectToPage("/Account/Verify", new { email, returnUrl = ReturnUrl });
    }
}
