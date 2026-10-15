namespace BestInTown.Models;

public class Topic
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime StartsOn { get; set; }   // Monday 00:00 UTC
    public DateTime EndsOn { get; set; }     // next Monday 00:00 UTC (exclusive)
    public List<Nomination> Nominations { get; set; } = new();
}

public class Place
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";   // case-insensitive uniqueness (Cyrillic-safe)
    public string? Neighborhood { get; set; }
}

public class Nomination
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
    public int PlaceId { get; set; }
    public Place Place { get; set; } = null!;
    public List<Vote> Votes { get; set; } = new();
}

public class Vote
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public Nomination Nomination { get; set; } = null!;
    public int TopicId { get; set; }
    public string VoterKey { get; set; } = "";   // anonymous cookie id (swap for a user id once you add accounts)
    public string IpHash { get; set; } = "";
    public DateTime CastAt { get; set; }
}

public class LeaderboardRow
{
    public int NominationId { get; set; }
    public string Name { get; set; } = "";
    public string? Neighborhood { get; set; }
    public int Votes { get; set; }
}

public class GuideEntry
{
    public string Title { get; set; } = "";
    public DateTime EndsOn { get; set; }
    public List<LeaderboardRow> Top { get; set; } = new();
}
