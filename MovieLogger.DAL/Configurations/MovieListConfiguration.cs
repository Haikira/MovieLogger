using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieLogger.Service.Entities;

namespace MovieLogger.DAL.Configurations
{
    public class MovieListConfiguration : IEntityTypeConfiguration<MovieList>
    {
        public void Configure(EntityTypeBuilder<MovieList> builder)
        {
            builder.ToTable("Lists");

            builder.Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(l => l.Description)
                .HasMaxLength(2000);

            builder.Property(l => l.CreatedAt)
                .IsRequired();

            builder.HasOne(l => l.User)
                .WithMany(u => u.Lists)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
