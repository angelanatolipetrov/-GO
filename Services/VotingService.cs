using BestInTown.Data;
using BestInTown.Models;
using Microsoft.EntityFrameworkCore;

namespace BestInTown.Services;

public class VotingService(AppDb db)
{
    private static readonly string[] Themes =
    {
        "Най-добрата пицария",
        "Най-тихото място за работа на открито",
        "Най-добрата занаятчийска пекарна",
        "Най-доброто кафе",
        "Най-добрият бургер",
        "Най-добрата сладкарница",
        "Най-доброто място за закуска",
        "Най-добрата книжарница",
    };

    private const int MaxVotesPerIpPerTopic = 3;

    // Returns the topic for the current week, creating it automatically on first access.
    public async Task<Topic> GetCurrentTopicAsync()
    {
        var now = DateTime.UtcNow;
        var topic = await db.Topics
            .Where(t => t.StartsOn <= now && t.EndsOn > now)
            .OrderByDescending(t => t.StartsOn)
            .FirstOrDefaultAsync();
        if (topic != null) return topic;

        var today = now.Date;
        var start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // Monday
        var week = System.Globalization.ISOWeek.GetWeekOfYear(today);
        topic = new Topic
        {
            Title = Themes[week % Themes.Length],
            StartsOn = start,
            EndsOn = start.AddDays(7)
        };
        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        return topic;
    }

    public Task<List<LeaderboardRow>> LeaderboardAsync(int topicId) =>
        db.Nominations
            .Where(n => n.TopicId == topicId)
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

    public async Task<string?> NominateAsync(int topicId, string? name, string? neighborhood)
    {
        name = name?.Trim() ?? "";
        neighborhood = string.IsNullOrWhiteSpace(neighborhood) ? null : neighborhood.Trim();
        if (name.Length < 2 || name.Length > 80) return "Името трябва да е между 2 и 80 символа.";

        var normalized = name.ToUpperInvariant();
        var place = await db.Places.FirstOrDefaultAsync(p => p.NormalizedName == normalized);
        if (place == null)
        {
            place = new Place { Name = name, NormalizedName = normalized, Neighborhood = neighborhood };
            db.Places.Add(place);
            await db.SaveChangesAsync();
        }

        if (await db.Nominations.AnyAsync(n => n.TopicId == topicId && n.PlaceId == place.Id))
            return "Това място вече е номинирано.";

        db.Nominations.Add(new Nomination { TopicId = topicId, PlaceId = place.Id });
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> VoteAsync(int nominationId, string voterKey, string ipHash)
    {
        var nom = await db.Nominations.Include(n => n.Topic).FirstOrDefaultAsync(n => n.Id == nominationId);
        if (nom == null) return "Номинацията не е намерена.";

        var now = DateTime.UtcNow;
        if (nom.Topic.StartsOn > now || nom.Topic.EndsOn <= now)
            return "Гласуването за тази тема е приключило.";

        if (await db.Votes.AnyAsync(v => v.TopicId == nom.TopicId && v.VoterKey == voterKey))
            return "Вече сте гласували тази седмица.";

        if (await db.Votes.CountAsync(v => v.TopicId == nom.TopicId && v.IpHash == ipHash) >= MaxVotesPerIpPerTopic)
            return "Достигнат е лимитът на гласове от тази мрежа.";

        db.Votes.Add(new Vote
        {
            NominationId = nom.Id,
            TopicId = nom.TopicId,
            VoterKey = voterKey,
            IpHash = ipHash,
            CastAt = now
        });

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return "Вече сте гласували тази седмица."; }
        return null;
    }

    // Catalog: top 3 for every finished topic.
    public async Task<List<GuideEntry>> GuideAsync()
    {
        var now = DateTime.UtcNow;
        var ended = await db.Topics.Where(t => t.EndsOn <= now)
            .OrderByDescending(t => t.EndsOn).Take(30).ToListAsync();

        var result = new List<GuideEntry>();
        foreach (var t in ended)
        {
            var top = (await LeaderboardAsync(t.Id)).Take(3).ToList();
            result.Add(new GuideEntry { Title = t.Title, EndsOn = t.EndsOn, Top = top });
        }
        return result;
    }
}
