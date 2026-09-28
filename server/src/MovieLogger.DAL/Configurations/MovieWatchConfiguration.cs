using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieLogger.Service.Entities;

namespace MovieLogger.DAL.Configurations
{
    public class MovieWatchConfiguration : IEntityTypeConfiguration<MovieWatch>
    {
        public void Configure(EntityTypeBuilder<MovieWatch> builder)
        {
            builder.Property(w => w.DateWatched)
                .IsRequired();

            builder.Property(w => w.Notes)
                .HasMaxLength(500);

            builder.Property(w => w.CreatedAt)
                .IsRequired();

            builder.ToTable(t => t.HasCheckConstraint("CK_MovieWatches_Rating", "[Rating] IS NULL OR [Rating] BETWEEN 1 AND 5"));

            builder.HasIndex(w => w.DateWatched);

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
