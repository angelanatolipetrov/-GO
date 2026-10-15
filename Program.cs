using BestInTown.Data;
using BestInTown.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=bestintown.db"));
builder.Services.AddScoped<VotingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();
}

app.UseRouting();
app.MapRazorPages();
app.Run();
