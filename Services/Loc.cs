namespace BestInTown.Services;

// Tiny BG/EN localizer. Language is stored in the "lang" cookie (default: bg).
public class Loc(IHttpContextAccessor accessor)
{
    public const string CookieName = "lang";
    public static bool IsSupported(string? code) => code is "bg" or "en";

    public string Lang => accessor.HttpContext?.Request.Cookies[CookieName] == "en" ? "en" : "bg";
    public bool IsEn => Lang == "en";
    public System.Globalization.CultureInfo Culture => new(IsEn ? "en-GB" : "bg-BG");

    public string this[string key] => S.TryGetValue(key, out var v) ? (IsEn ? v.en : v.bg) : key;
    public bool Has(string key) => S.ContainsKey(key);
    public string Format(string key, params object[] args) => string.Format(this[key], args);
    public string Pick(string bg, string? en) => IsEn && !string.IsNullOrWhiteSpace(en) ? en! : bg;

    private static readonly Dictionary<string, (string bg, string en)> S = new()
    {
        // layout
        ["site.name"] = ("Най-доброто в града", "Best in Town"),
        ["nav.week"] = ("Тази седмица", "This week"),
        ["nav.guide"] = ("Градски гид", "City guide"),
        ["nav.login"] = ("Вход", "Log in"),
        ["nav.register"] = ("Регистрация", "Register"),
        ["nav.logout"] = ("Изход", "Log out"),
        // home
        ["home.closes"] = ("Гласуването приключва на {0}. Един глас на човек седмично.", "Voting ends on {0}. One vote per person per week."),
        ["home.all"] = ("Всички", "All"),
        ["home.empty"] = ("Все още няма номинации. Бъдете първите!", "No nominations yet. Be the first!"),
        ["btn.vote"] = ("Гласувай", "Vote"),
        ["btn.voted"] = ("✓ Вашият глас", "✓ Your vote"),
        ["btn.loginToVote"] = ("Влезте, за да гласувате", "Log in to vote"),
        ["nom.h"] = ("Номинирай място", "Nominate a place"),
        ["nom.name"] = ("Име на мястото", "Place name"),
        ["nom.hood"] = ("Квартал (по избор)", "Neighborhood (optional)"),
        ["nom.hoodSelect"] = ("Квартал…", "Neighborhood…"),
        ["nom.btn"] = ("Номинирай", "Nominate"),
        ["nom.login"] = ("Влезте, за да номинирате място.", "Log in to nominate a place."),
        ["nom.choose"] = ("— Избери място в Бургас —", "— Choose a place in Burgas —"),
        ["nom.other"] = ("✍ Друго място (напиши името)", "✍ Another place (type the name)"),
        ["cat.pizza"] = ("Пицарии", "Pizza"),
        ["cat.coffee"] = ("Кафе", "Coffee"),
        ["cat.bakery"] = ("Пекарни", "Bakeries"),
        ["cat.burger"] = ("Бургери", "Burgers"),
        ["cat.sweets"] = ("Сладкарници", "Sweets & desserts"),
        ["cat.brunch"] = ("Закуска и бранч", "Breakfast & brunch"),
        ["cat.restaurant"] = ("Ресторанти", "Restaurants"),
        ["cat.books"] = ("Книжарници", "Bookstores"),
        ["cat.parks"] = ("Паркове и градини", "Parks & gardens"),
        ["votes"] = ("гласа", "votes"),
        ["home.endsIn"] = ("Приключва след", "Ends in"),
        ["home.places"] = ("места", "places"),
        ["home.lastWinner"] = ("Победител миналата седмица", "Last week's winner"),
        ["search.ph"] = ("Търси място…", "Search places…"),
        ["share.btn"] = ("Сподели", "Share"),
        ["share.copied"] = ("Линкът е копиран!", "Link copied!"),
        ["unit.d"] = ("д", "d"),
        ["unit.h"] = ("ч", "h"),
        ["unit.m"] = ("мин", "min"),
        ["footer"] = ("Правено с ♥ от местни за местни", "Made with ♥ by locals, for locals"),
        ["guide.winner"] = ("Победител", "Winner"),
        // messages
        ["msg.voted"] = ("Гласът ви е приет!", "Your vote has been counted!"),
        ["msg.nominated"] = ("Мястото е номинирано!", "Place nominated!"),
        ["msg.loginRequired"] = ("Моля, влезте в профила си.", "Please log in first."),
        ["err.generic"] = ("Възникна грешка. Опитайте отново.", "Something went wrong. Please try again."),
        ["err.nom.notfound"] = ("Номинацията не е намерена.", "Nomination not found."),
        ["err.vote.closed"] = ("Гласуването за тази тема е приключило.", "Voting for this topic has ended."),
        ["err.vote.already"] = ("Вече сте гласували тази седмица.", "You have already voted this week."),
        ["err.vote.ip"] = ("Достигнат е лимитът на гласове от тази мрежа.", "The vote limit for this network has been reached."),
        ["err.nom.name"] = ("Името трябва да е между 2 и 80 символа.", "The name must be 2–80 characters."),
        ["err.nom.hood"] = ("Кварталът е твърде дълъг.", "The neighborhood name is too long."),
        ["err.nom.limit"] = ("Достигнахте седмичния лимит за номинации.", "You have reached this week's nomination limit."),
        ["err.nom.dup"] = ("Това място вече е номинирано.", "This place has already been nominated."),
        // guide / error
        ["guide.h"] = ("Градски гид", "City guide"),
        ["guide.sub"] = ("Победителите от предишните седмици, избрани от местните.", "Winners from previous weeks, chosen by locals."),
        ["guide.empty"] = ("Първите резултати ще се появят след края на първата седмица.", "The first results will appear after the first week ends."),
        ["error.h"] = ("Възникна грешка", "Something went wrong"),
        ["error.p"] = ("Моля, опитайте отново след малко.", "Please try again in a moment."),
        // admin
        ["admin.h"] = ("Модерация", "Moderation"),
        ["admin.topic"] = ("Тема на седмицата", "This week's topic"),
        ["admin.titleBg"] = ("Заглавие (BG)", "Title (BG)"),
        ["admin.titleEn"] = ("Заглавие (EN)", "Title (EN)"),
        ["admin.save"] = ("Запази", "Save"),
        ["admin.noms"] = ("Номинации", "Nominations"),
        ["admin.none"] = ("Няма номинации.", "No nominations."),
        ["admin.delete"] = ("Изтрий", "Delete"),
        // account
        ["acct.email"] = ("Имейл", "Email"),
        ["acct.password"] = ("Парола", "Password"),
        ["acct.confirm"] = ("Повторете паролата", "Confirm password"),
        ["acct.remember"] = ("Запомни ме", "Remember me"),
        ["login.h"] = ("Вход", "Log in"),
        ["login.btn"] = ("Влез", "Log in"),
        ["login.noacct"] = ("Нямате профил?", "No account yet?"),
        ["login.invalid"] = ("Грешен имейл или парола.", "Invalid email or password."),
        ["login.locked"] = ("Профилът е временно заключен. Опитайте по-късно.", "Account temporarily locked. Try again later."),
        ["reg.h"] = ("Регистрация", "Register"),
        ["reg.btn"] = ("Създай профил", "Create account"),
        ["reg.have"] = ("Вече имате профил?", "Already have an account?"),
        ["err.email"] = ("Въведете валиден имейл.", "Enter a valid email address."),
        ["err.pw.mismatch"] = ("Паролите не съвпадат.", "Passwords do not match."),
        ["idErr.DuplicateUserName"] = ("Вече има профил с този имейл.", "An account with this email already exists."),
        ["idErr.DuplicateEmail"] = ("Вече има профил с този имейл.", "An account with this email already exists."),
        ["idErr.InvalidEmail"] = ("Въведете валиден имейл.", "Enter a valid email address."),
        ["idErr.PasswordTooShort"] = ("Паролата трябва да е поне 8 символа.", "The password must be at least 8 characters."),
        ["idErr.PasswordRequiresDigit"] = ("Паролата трябва да съдържа цифра.", "The password must contain a digit."),
    };
}
