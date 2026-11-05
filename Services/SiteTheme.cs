namespace BestInTown.Services;

// Site colors chosen with the palette button in the header. Saved in cookies, like the language.
public static class SiteTheme
{
    public const string AccentCookie = "accent", ModeCookie = "mode";

    // name -> swatch color shown in the menu (the real values are in site.css)
    public static readonly (string Name, string Swatch)[] Accents =
    {
        ("red", "#e3212b"), ("orange", "#f97316"), ("green", "#16a34a"),
        ("blue", "#2563eb"), ("purple", "#8b5cf6"), ("pink", "#e11d74"),
    };

    public static bool IsAccent(string? v) => Accents.Any(a => a.Name == v);
    public static bool IsMode(string? v) => v is "dark" or "light";

    public static string Accent(HttpRequest r) => IsAccent(r.Cookies[AccentCookie]) ? r.Cookies[AccentCookie]! : "red";
    public static string Mode(HttpRequest r) => r.Cookies[ModeCookie] == "light" ? "light" : "dark";
}
