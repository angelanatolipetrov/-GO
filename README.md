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

## Проверка на имейл при регистрация
`Services/EmailChecker.cs`: формат, временни имейли (mailinator, yopmail и др.), правописни грешки
(gmial.com → gmail.com) и DNS проверка, че домейнът съществува и има пощенски сървър (MX).
Ако DNS не отговаря, регистрацията не се блокира.

## Не е включено
Потвърждение на имейл и забравена парола (нужен е имейл доставчик), CAPTCHA при регистрация.
