using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieLogger.Service.Entities;

namespace MovieLogger.DAL.Configurations
{
    public class WatchlistItemConfiguration : IEntityTypeConfiguration<WatchlistItem>
    {
        public void Configure(EntityTypeBuilder<WatchlistItem> builder)
        {
            builder.ToTable("WatchlistItems");

            builder.Property(w => w.DateAdded)
                .IsRequired();

            builder.HasIndex(w => new { w.UserId, w.MovieId })
                .IsUnique();

            builder.HasOne(w => w.User)
                .WithMany(u => u.WatchlistItems)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.Movie)
                .WithMany(m => m.WatchlistItems)
                .HasForeignKey(w => w.MovieId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
