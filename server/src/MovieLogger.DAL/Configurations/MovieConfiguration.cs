using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieLogger.Service.Entities;

namespace MovieLogger.DAL.Configurations
{
    public class MovieConfiguration : IEntityTypeConfiguration<Movie>
    {
        public void Configure(EntityTypeBuilder<Movie> builder)
        {
            builder.Property(m => m.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.ReleaseYear)
                .IsRequired();

            builder.Property(m => m.RuntimeMinutes);

            builder.Property(m => m.Director)
                .HasMaxLength(200);

            builder.Property(m => m.Synopsis)
                .HasMaxLength(2000);

            builder.Property(m => m.PosterImageUrl)
                .HasMaxLength(2000);

            builder.Property(m => m.CreatedAt)
                .IsRequired();

            builder.HasIndex(m => m.Title);

            builder.HasOne(m => m.CreatedBy)
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(m => m.Genres)
                .WithMany(g => g.Movies)
                .UsingEntity<MovieGenre>(
                    j => j.HasOne(mg => mg.Genre).WithMany().HasForeignKey(mg => mg.GenreId),
                    j => j.HasOne(mg => mg.Movie).WithMany().HasForeignKey(mg => mg.MovieId),
                    j =>
                    {
                        j.HasKey(mg => new { mg.MovieId, mg.GenreId });
                        j.ToTable("MovieGenres");
                    });
        }
    }
}
