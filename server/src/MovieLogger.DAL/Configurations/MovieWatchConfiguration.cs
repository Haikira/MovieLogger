using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieLogger.Service.Entities;

namespace MovieLogger.DAL.Configurations
{
    public class MovieWatchConfiguration : IEntityTypeConfiguration<MovieWatch>
    {
        public void Configure(EntityTypeBuilder<MovieWatch> builder)
        {
            builder.Property(w => w.WatchedAt)
                .IsRequired();

            builder.Property(w => w.Score)
                .HasPrecision(3, 1);

            builder.Property(w => w.Review)
                .HasMaxLength(4000);

            builder.HasOne(w => w.User)
                .WithMany(u => u.MovieWatches)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.Movie)
                .WithMany(m => m.MovieWatches)
                .HasForeignKey(w => w.MovieId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
