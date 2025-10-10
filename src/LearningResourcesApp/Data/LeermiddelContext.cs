using LearningResourcesApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LearningResourcesApp.Data;

public class LeermiddelContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Leermiddel> Leermiddelen { get; set; }
    public DbSet<Reactie> Reacties { get; set; }

    public LeermiddelContext(DbContextOptions<LeermiddelContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configureer de relatie tussen Leermiddel en Reactie
        modelBuilder.Entity<Leermiddel>()
            .HasMany(l => l.Reacties)
            .WithOne()
            .HasForeignKey("LeermiddelId")
            .OnDelete(DeleteBehavior.Cascade);

        // Index voor snellere zoekopdrachten
        modelBuilder.Entity<Leermiddel>()
            .HasIndex(l => l.AangemaaktOp);

        modelBuilder.Entity<Reactie>()
            .HasIndex(r => r.AangemaaktOp);
    }
}
