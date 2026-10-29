using BestInTown.Data;
using BestInTown.Models;
using Microsoft.EntityFrameworkCore;

namespace BestInTown.Services;

// Methods return null on success or a localization key (see Loc) describing the problem.
public class VotingService(AppDb db)
{
    private static readonly (string Bg, string En)[] Themes =
    {
        ("Най-добрата пицария", "Best pizzeria"),
        ("Най-тихото място за работа на открито", "Quietest spot to work outdoors"),
        ("Най-добрата занаятчийска пекарна", "Best artisan bakery"),
        ("Най-доброто кафе", "Best coffee"),
        ("Най-добрият бургер", "Best burger"),
        ("Най-добрата сладкарница", "Best pastry shop"),
        ("Най-доброто място за закуска", "Best breakfast spot"),
        ("Най-добрата книжарница", "Best bookstore"),
    };

    // Soft signal only: accounts are the main limit, and many people share one IP (homes, offices).
    private const int MaxVotesPerIpPerTopic = 10;
    private const int MaxNominationsPerUserPerTopic = 3;

    // Topic for the current week (Mon–Mon, UTC); created automatically on first access.
    public async Task<Topic> GetCurrentTopicAsync()
    {
        var now = DateTime.UtcNow;
        var topic = await FindCurrentAsync(now);
        if (topic != null) return topic;

        var today = now.Date;
        var start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var week = System.Globalization.ISOWeek.GetWeekOfYear(today);
        var theme = Themes[week % Themes.Length];
        topic = new Topic { Title = theme.Bg, TitleEn = theme.En, StartsOn = start, EndsOn = start.AddDays(7) };
        db.Topics.Add(topic);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)   // another request created it first
        {
            db.Entry(topic).State = EntityState.Detached;
            topic = await FindCurrentAsync(now) ?? throw new InvalidOperationException("Topic creation failed.");
        }
        return topic;
    }

    private Task<Topic?> FindCurrentAsync(DateTime now) =>
        db.Topics.Where(t => t.StartsOn <= now && t.EndsOn > now)
                 .OrderByDescending(t => t.StartsOn).FirstOrDefaultAsync();

    public Task<List<LeaderboardRow>> LeaderboardAsync(int topicId, string? neighborhood = null) =>
        db.Nominations
            .Where(n => n.TopicId == topicId)
            .Where(n => neighborhood == null || n.Place.Neighborhood == neighborhood)
            .OrderByDescending(n => n.Votes.Count)
            .ThenBy(n => n.Place.Name)
            .Select(n => new LeaderboardRow
            {
                NominationId = n.Id,
                Name = n.Place.Name,
                Neighborhood = n.Place.Neighborhood,
                Votes = n.Votes.Count
            })
            .ToListAsync();

    public Task<List<string>> NeighborhoodsAsync(int topicId) =>
        db.Nominations.Where(n => n.TopicId == topicId && n.Place.Neighborhood != null)
            .Select(n => n.Place.Neighborhood!).Distinct().OrderBy(x => x).ToListAsync();

    public Task<int?> MyVoteAsync(int topicId, string? userId) =>
        userId == null
            ? Task.FromResult<int?>(null)
            : db.Votes.Where(v => v.TopicId == topicId && v.VoterKey == userId)
                      .Select(v => (int?)v.NominationId).FirstOrDefaultAsync();

    public async Task<string?> NominateAsync(int topicId, string? name, string? neighborhood, string userId)
    {
        name = name?.Trim() ?? "";
        neighborhood = string.IsNullOrWhiteSpace(neighborhood) ? null : neighborhood.Trim();
        if (name.Length < 2 || name.Length > 80) return "err.nom.name";
        if (neighborhood is { Length: > 60 }) return "err.nom.hood";

        if (await db.Nominations.CountAsync(n => n.TopicId == topicId && n.NominatorKey == userId)
            >= MaxNominationsPerUserPerTopic)
            return "err.nom.limit";

        var normalized = name.ToUpperInvariant();
        var place = await db.Places.FirstOrDefaultAsync(p => p.NormalizedName == normalized);
        if (place == null)
        {
            place = new Place { Name = name, NormalizedName = normalized, Neighborhood = neighborhood };
            db.Places.Add(place);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                db.Entry(place).State = EntityState.Detached;
                place = await db.Places.FirstOrDefaultAsync(p => p.NormalizedName == normalized);
                if (place == null) return "err.generic";
            }
        }

        if (await db.Nominations.AnyAsync(n => n.TopicId == topicId && n.PlaceId == place.Id))
            return "err.nom.dup";

        var nom = new Nomination { TopicId = topicId, PlaceId = place.Id, NominatorKey = userId };
        db.Nominations.Add(nom);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            db.Entry(nom).State = EntityState.Detached;
            return "err.nom.dup";
        }
        return null;
    }

    public async Task<string?> VoteAsync(int nominationId, string userId, string ipHash)
    {
        var nom = await db.Nominations.Include(n => n.Topic).FirstOrDefaultAsync(n => n.Id == nominationId);
        if (nom == null) return "err.nom.notfound";

        var now = DateTime.UtcNow;
        if (nom.Topic.StartsOn > now || nom.Topic.EndsOn <= now) return "err.vote.closed";

        if (await db.Votes.AnyAsync(v => v.TopicId == nom.TopicId && v.VoterKey == userId))
            return "err.vote.already";

        if (await db.Votes.CountAsync(v => v.TopicId == nom.TopicId && v.IpHash == ipHash) >= MaxVotesPerIpPerTopic)
            return "err.vote.ip";

        db.Votes.Add(new Vote
        {
            NominationId = nom.Id, TopicId = nom.TopicId,
            VoterKey = userId, IpHash = ipHash, CastAt = now
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return "err.vote.already"; }
        return null;
    }

    // ---- moderation (used by /Admin) ----
    public async Task DeleteNominationAsync(int nominationId)
    {
        var nom = await db.Nominations.Include(n => n.Votes).FirstOrDefaultAsync(n => n.Id == nominationId);
        if (nom == null) return;
        db.Votes.RemoveRange(nom.Votes);
        db.Nominations.Remove(nom);
        await db.SaveChangesAsync();
    }

    public async Task RenameTopicAsync(int topicId, string? title, string? titleEn)
    {
        title = title?.Trim();
        titleEn = string.IsNullOrWhiteSpace(titleEn) ? null : titleEn.Trim();
        if (string.IsNullOrEmpty(title) || title.Length > 120 || titleEn is { Length: > 120 }) return;
        var t = await db.Topics.FindAsync(topicId);
        if (t == null) return;
        t.Title = title;
        t.TitleEn = titleEn;
        await db.SaveChangesAsync();
    }

    // Winner of the most recent finished topic (null if nothing finished yet or no votes).
    public async Task<GuideEntry?> LastWinnerAsync()
    {
        var now = DateTime.UtcNow;
        var t = await db.Topics.Where(x => x.EndsOn <= now).OrderByDescending(x => x.EndsOn).FirstOrDefaultAsync();
        if (t == null) return null;
        var top = (await LeaderboardAsync(t.Id)).Where(r => r.Votes > 0).Take(1).ToList();
        return top.Count == 0 ? null : new GuideEntry { Title = t.Title, TitleEn = t.TitleEn, EndsOn = t.EndsOn, Top = top };
    }

    // Catalog: top 3 of every finished topic.
    public async Task<List<GuideEntry>> GuideAsync()
    {
        var now = DateTime.UtcNow;
        var ended = await db.Topics.Where(t => t.EndsOn <= now)
            .OrderByDescending(t => t.EndsOn).Take(30).ToListAsync();

        var result = new List<GuideEntry>();
        foreach (var t in ended)
        {
            var top = (await LeaderboardAsync(t.Id)).Where(r => r.Votes > 0).Take(3).ToList();
            result.Add(new GuideEntry { Title = t.Title, TitleEn = t.TitleEn, EndsOn = t.EndsOn, Top = top });
        }
        return result;
    }
}
