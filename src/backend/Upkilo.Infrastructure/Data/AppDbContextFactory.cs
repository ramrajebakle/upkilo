using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Upkilo.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../Upkilo.API");
        
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Database=upkilo;Username=upkilo;Password=upkilo_dev_2026";

        builder.UseNpgsql(connectionString, b => b.MigrationsAssembly("Upkilo.Infrastructure"));

        return new AppDbContext(builder.Options);
    }
}
