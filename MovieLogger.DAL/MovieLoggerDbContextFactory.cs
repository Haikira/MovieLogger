using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MovieLogger.DAL
{
    public class MovieLoggerDbContextFactory : IDesignTimeDbContextFactory<MovieLoggerDbContext>
    {
        public MovieLoggerDbContext CreateDbContext(string[] args)
        {
            // dotnet ef executes design-time operations against the startup project's build
            // output, so appsettings.json (copied there by the Api's Web SDK) resolves from
            // AppContext.BaseDirectory. Fall back to the invoking shell's cwd for safety.
            var basePath = File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
                ? AppContext.BaseDirectory
                : Directory.GetCurrentDirectory();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<MovieLoggerDbContext>();
            optionsBuilder.UseSqlite(configuration.GetConnectionString("MovieLoggerDb"));

            return new MovieLoggerDbContext(optionsBuilder.Options);
        }
    }
}
