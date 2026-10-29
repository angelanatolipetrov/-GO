namespace BestInTown.Data;

public record CatalogPlace(string Category, string Name, string? Neighborhood);

// Curated list of places in Burgas for the nomination dropdown (compiled from Google Maps, Oct 2026).
// Edit freely: add/remove entries or change categories. Category labels live in Services/Loc.cs ("cat.*").
public static class PlaceCatalog
{
    public static readonly IReadOnlyList<CatalogPlace> All = new CatalogPlace[]
    {
        new("pizza", "Romance Pub & Pizza", "Център"),
        new("pizza", "Mio Pizza", "Център"),
        new("pizza", "Pizza Shop", "Център"),
        new("pizza", "Grand Italia", "Лазур"),

        new("coffee", "DABOV Specialty Coffee", "Център"),
        new("coffee", "The Coffee Room", "Център"),
        new("coffee", "Specialty Coffee Albatros (DABOV)", "Северен плаж"),
        new("coffee", "New Coffee World", "Братя Миладинови"),

        new("bakery", "BAKERY by Thaka", "Център"),
        new("bakery", "Bella Bonita Bakery", "Център"),
        new("bakery", "Tezgiah Bread & Bakery", "Възраждане"),
        new("bakery", "Харна Пекарна", "Възраждане"),
        new("bakery", "Marketo Bakery & Bistro", "Братя Миладинови"),

        new("burger", "King Kong Burger", "Център"),
        new("burger", "Burger & Pizza Bella Bonita", "Център"),
        new("burger", "Burgers House", "Братя Миладинови"),
        new("burger", "Boss Burger & Grill", "Славейков"),
        new("burger", "BURGERIA", "Северна промишлена зона"),

        new("sweets", "Valentinka Bakery & Sweets", "Лазур"),
        new("sweets", "Fanani's Vegan Pastry Shop", "Център"),
        new("sweets", "Сладкарски бутик Креми", "Център"),

        new("brunch", "Café de Paris", "Център"),
        new("brunch", "Butlers Coffee & Kitchen", "Център"),
        new("brunch", "Djezve Brunch & Coffee", "Център"),

        new("restaurant", "Incanto", "Център"),
        new("restaurant", "Cachot Kitchen & Bar", "Център"),

        new("books", "Helikon", "Център"),
        new("books", "Orange", "Център"),
        new("books", "Zalivi", "Братя Миладинови"),
        new("books", "Книжарница Сиела", "Северна промишлена зона"),

        new("parks", "Морската градина", "Център"),
        new("parks", "Градина на розите", "Център"),
        new("parks", "Борисова градина", "Възраждане"),
        new("parks", "Парк Славейков", "Славейков"),
    };

    public static CatalogPlace? Find(string? name) =>
        string.IsNullOrEmpty(name) ? null : All.FirstOrDefault(p => p.Name == name);
}
