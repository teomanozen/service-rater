using Microsoft.EntityFrameworkCore;
using RatingService.Models;

namespace RatingService.Data;
public class RatingDbContext : DbContext
{
    public RatingDbContext(DbContextOptions<RatingDbContext> options) : base(options) { }
    
    public DbSet<Rating> Ratings { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rating>(entity =>
        {
            // Set max length for Comment (can't easily do with nullable string attributes)
            entity.Property(e => e.Comment)
                .HasMaxLength(1000);
            
            // Single index
            entity.HasIndex(e => e.ServiceProviderId)
                .HasDatabaseName("idx_ratings_service_provider_id");
        });
        
        base.OnModelCreating(modelBuilder);
    }
}