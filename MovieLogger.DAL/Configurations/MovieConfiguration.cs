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

            builder.Property(m => m.ReleaseDate)
                .IsRequired();

            builder.Property(m => m.Director)
                .HasMaxLength(200);

            builder.Property(m => m.Description)
                .HasMaxLength(2000);

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
