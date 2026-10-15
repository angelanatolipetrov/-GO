using BestInTown.Models;
using Microsoft.EntityFrameworkCore;

namespace BestInTown.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Nomination> Nominations => Set<Nomination>();
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Place>().HasIndex(p => p.NormalizedName).IsUnique();
        b.Entity<Nomination>().HasIndex(n => new { n.TopicId, n.PlaceId }).IsUnique();
        // one vote per voter per weekly topic, enforced by the database
        b.Entity<Vote>().HasIndex(v => new { v.TopicId, v.VoterKey }).IsUnique();
    }
}
