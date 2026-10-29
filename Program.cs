using System.Security.Cryptography;
using System.Text;
using BestInTown.Data;
using BestInTown.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Loc>();
builder.Services.AddScoped<VotingService>();
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=bestintown.db"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 8;
    o.Password.RequireDigit = true;
    o.Password.RequireLowercase = false;
    o.Password.RequireUppercase = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
}).AddEntityFrameworkStores<AppDb>();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Login";
    o.ExpireTimeSpan = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;
});

// Correct client IP behind a reverse proxy. For proxies not on localhost, add them to KnownProxies.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "same-origin";
    h["Content-Security-Policy"] = "default-src 'self'; style-src 'self' 'unsafe-inline'";
    await next();
});

// /Admin is protected with HTTP Basic auth (password from Admin:Password / env Admin__Password).
// If no password is configured, /Admin is disabled (404).
var adminPassword = builder.Configuration["Admin:Password"];

static bool IsAuthorized(string? header, string password)
{
    if (header is null || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;
    try
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(header[6..].Trim()));
        var given = raw[(raw.IndexOf(':') + 1)..];
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(password));
    }
    catch (FormatException) { return false; }
}

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/Admin"))
    {
        if (string.IsNullOrEmpty(adminPassword)) { ctx.Response.StatusCode = 404; return; }
        if (!IsAuthorized(ctx.Request.Headers.Authorization, adminPassword))
        {
            ctx.Response.Headers.WWWAuthenticate = "Basic realm=\"Admin\"";
            ctx.Response.StatusCode = 401;
            return;
        }
    }
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Language switch: /lang/bg or /lang/en, then back to the page the user was on.
app.MapGet("/lang/{code}", (string code, string? returnUrl, HttpContext ctx) =>
{
    if (Loc.IsSupported(code))
        ctx.Response.Cookies.Append(Loc.CookieName, code, new CookieOptions
        {
            IsEssential = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });

    var local = !string.IsNullOrEmpty(returnUrl) && returnUrl[0] == '/'
                && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'));
    return Results.LocalRedirect(local ? returnUrl! : "/");
});

app.MapRazorPages();
app.Run();
