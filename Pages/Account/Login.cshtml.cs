using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BestInTown.Services;

namespace BestInTown.Pages.Account;

public class LoginModel(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users, EmailCodes codes,
                        ILogger<LoginModel> log, Loc L) : PageModel
{
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Password { get; set; }
    [BindProperty] public bool RememberMe { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? Error { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            Error = L["login.invalid"];
            return Page();
        }

        var result = await signIn.PasswordSignInAsync(Email.Trim(), Password, RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");

        // Right password, but the email was never confirmed: send a code and go to the code page.
        // (Identity refuses unconfirmed accounts before checking the password, so check it here.)
        if (result.IsNotAllowed && await users.FindByEmailAsync(Email.Trim()) is { EmailConfirmed: false } user
            && await users.CheckPasswordAsync(user, Password))
        {
            try { if (await codes.SendAsync(user)) TempData["Msg"] = L["verify.sent"]; }
            catch (Exception ex) { log.LogError(ex, "Could not send verification code to {Email}", user.Email); }
            return RedirectToPage("/Account/Verify", new { email = user.Email, returnUrl = ReturnUrl });
        }

        Error = result.IsLockedOut ? L["login.locked"] : L["login.invalid"];
        return Page();
    }
}
