using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using ArenaService.Shared.Data;

namespace ArenaService.Migrations
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ArenaDbContext>
    {
        public ArenaDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ArenaDbContext>();
            optionsBuilder
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention();

            return new ArenaDbContext(optionsBuilder.Options);
        }
    }
}
