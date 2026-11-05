using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BestInTown.Services;

namespace BestInTown.Pages.Account;

public class VerifyModel(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, EmailCodes codes,
                         ILogger<VerifyModel> log, Loc L) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Email { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty] public string? Code { get; set; }
    public string? Error { get; set; }

    public IActionResult OnGet() => string.IsNullOrWhiteSpace(Email) ? RedirectToPage("/Account/Register") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await FindUnconfirmedAsync();
        if (user == null) { Error = L["verify.wrong"]; return Page(); }   // same message: don't reveal who has an account

        switch (await codes.CheckAsync(user, Code))
        {
            case CodeResult.Ok:
                await signIn.SignInAsync(user, isPersistent: true);
                TempData["Msg"] = L["verify.done"];
                return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
            case CodeResult.Expired: Error = L["verify.expired"]; break;
            case CodeResult.LockedOut: Error = L["verify.locked"]; break;
            default: Error = L["verify.wrong"]; break;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync()
    {
        var user = await FindUnconfirmedAsync();
        if (user != null)
        {
            try
            {
                if (!await codes.SendAsync(user)) { Error = L["verify.wait"]; return Page(); }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Could not send verification code to {Email}", user.Email);
                Error = L["verify.sendFailed"];
                return Page();
            }
        }
        TempData["Msg"] = L["verify.sent"];
        return RedirectToPage(new { email = Email, returnUrl = ReturnUrl });
    }

    private async Task<IdentityUser?> FindUnconfirmedAsync()
    {
        if (string.IsNullOrWhiteSpace(Email)) return null;
        var user = await users.FindByEmailAsync(Email.Trim());
        return user is { EmailConfirmed: false } ? user : null;
    }
}
