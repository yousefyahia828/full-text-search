using FullText.API.Database.Configurations;
using FullText.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FullText.API.Database;

internal sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Blog> Blogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BlogConfiguration());
    }
}
