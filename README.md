# Най-доброто в града / Best in Town (ASP.NET Core 10, Razor Pages, SQLite, Identity)

    dotnet run

Езици: BG (по подразбиране) и EN – превключвател в навигацията (бисквитка `lang`). Всички текстове са в `Services/Loc.cs`.
Профили: регистрация/вход с имейл и парола (ASP.NET Core Identity). Гласуване и номиниране само за влезли потребители.

Схемата се създава с `EnsureCreated`. След промяна на модела изтрийте `bestintown.db`
(за продукция преминете към EF миграции).

## Конфигурация (appsettings.json или env променливи)
- `Security__IpSalt` – задължително сменете стойността.
- `Admin__Password` – парола за `/Admin` (потребителско име: произволно). Без нея `/Admin` е изключен. Само през HTTPS.
- `Neighborhoods` – списък с квартали, напр. `["Център","Лазур"]`.
- `ConnectionStrings__Default` – път до SQLite файла.

## Зад reverse proxy
Добавете адреса на proxy-то в `ForwardedHeadersOptions.KnownProxies` (Program.cs), иначе IP лимитът ще вижда адреса на proxy-то.

## Не е включено
Потвърждение на имейл и забравена парола (нужен е имейл доставчик), CAPTCHA при регистрация.
