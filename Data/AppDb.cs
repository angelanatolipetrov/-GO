using BestInTown.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BestInTown.Data;

public class AppDb(DbContextOptions<AppDb> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Nomination> Nominations => Set<Nomination>();
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);   // required: configures the Identity tables
        b.Entity<Place>().HasIndex(p => p.NormalizedName).IsUnique();
        b.Entity<Nomination>().HasIndex(n => new { n.TopicId, n.PlaceId }).IsUnique();
        // one vote per account per weekly topic, enforced by the database
        b.Entity<Vote>().HasIndex(v => new { v.TopicId, v.VoterKey }).IsUnique();
    }
}
