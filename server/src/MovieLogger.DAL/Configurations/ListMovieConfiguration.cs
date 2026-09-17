using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieLogger.Service.Entities;

namespace MovieLogger.DAL.Configurations
{
    public class ListMovieConfiguration : IEntityTypeConfiguration<ListMovie>
    {
        public void Configure(EntityTypeBuilder<ListMovie> builder)
        {
            builder.ToTable("ListMovies");

            builder.HasKey(lm => new { lm.ListId, lm.MovieId });

            builder.Property(lm => lm.AddedAt)
                .IsRequired();

            builder.HasOne(lm => lm.List)
                .WithMany(l => l.ListMovies)
                .HasForeignKey(lm => lm.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(lm => lm.Movie)
                .WithMany(m => m.ListMovies)
                .HasForeignKey(lm => lm.MovieId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
