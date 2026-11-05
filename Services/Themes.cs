using BestInTown.Models;

namespace BestInTown.Services;

// Text and artwork for each weekly theme. Art: "food", "outdoor" or "books" (picks the line drawing and icon).
public record Theme(string Bg, string En, string TaglineBg, string TaglineEn,
                   string CategoryBg, string CategoryEn, string AboutBg, string AboutEn, string Art);

public static class Themes
{
    public static readonly Theme[] All =
    {
        new("Най-добрата пицария", "Best pizzeria",
            "Хрупкава коричка и разтопено сирене 🍕", "Crispy crust and melted cheese 🍕",
            "Пица на всеки ъгъл", "Pizza on every corner",
            "Коя пицария в Бургас прави най-добрата пица? Гласувайте за любимата си тази седмица!", "Which pizzeria in Burgas makes the best pizza? Vote for your favourite this week!", "food"),
        new("Най-тихото място за работа на открито", "Quietest spot to work outdoors",
            "Лаптоп, сянка и морски бриз 🌿", "A laptop, some shade and a sea breeze 🌿",
            "Тишина и свеж въздух", "Quiet and fresh air",
            "Къде в Бургас се работи най-спокойно навън? Покажете ни вашето място!", "Where in Burgas is it calmest to work outside? Show us your spot!", "outdoor"),
        new("Най-добрата занаятчийска пекарна", "Best artisan bakery",
            "Мирисът на топъл хляб 🥖", "The smell of warm bread 🥖",
            "Хляб с душа", "Bread with soul",
            "Квас, хрупкава коричка и ранно утро. Коя пекарна печели вашето сърце?", "Sourdough, crispy crust and early mornings. Which bakery wins your heart?", "food"),
        new("Най-доброто кафе", "Best coffee",
            "Денят започва с добро кафе ☕", "Every day starts with good coffee ☕",
            "Кафе с характер", "Coffee with character",
            "От еспресо до филтър – къде в Бургас пият най-доброто кафе?", "From espresso to filter – where does Burgas drink the best coffee?", "food"),
        new("Най-добрият бургер", "Best burger",
            "Сочно, голямо и с много сос 🍔", "Juicy, big and extra saucy 🍔",
            "Бургер мания", "Burger mania",
            "Кой бургер в Бургас си заслужава всяка хапка? Гласувайте тази седмица!", "Which burger in Burgas is worth every bite? Vote this week!", "food"),
        new("Най-добрата сладкарница", "Best pastry shop",
            "Сладките моменти започват тук 🍰", "Sweet moments start here 🍰",
            "Сладки изкушения", "Sweet temptations",
            "Насладете се на най-добрите десерти в града и гласувайте за любимата си сладкарница тази седмица!", "Enjoy the best desserts in town and vote for your favourite pastry shop this week!", "food"),
        new("Най-доброто място за закуска", "Best breakfast spot",
            "Добро утро, Бургас 🥐", "Good morning, Burgas 🥐",
            "Закуска като за празник", "Breakfast like a holiday",
            "Палачинки, яйца или баница – къде е най-добрата закуска в града?", "Pancakes, eggs or banitsa – where is the best breakfast in town?", "food"),
        new("Най-добрата книжарница", "Best bookstore",
            "Една страница по-далеч от шума 📚", "One page away from the noise 📚",
            "Между кориците", "Between the covers",
            "Кое е любимото ви място за книги в Бургас? Гласувайте за него!", "What's your favourite place for books in Burgas? Vote for it!", "books"),
    };

    // Matches by title, so a topic renamed in /Admin falls back to neutral text.
    public static Theme? For(Topic t) =>
        All.FirstOrDefault(x => x.Bg == t.Title || (t.TitleEn != null && x.En == t.TitleEn));
}
