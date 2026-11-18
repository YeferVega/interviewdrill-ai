using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed class InterviewDrillDbContextFactory : IDesignTimeDbContextFactory<InterviewDrillDbContext>
{
    public InterviewDrillDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("INTERVIEWDRILL_CONNECTION_STRING")
            ?? GetDefaultConnectionString();

        var options = new DbContextOptionsBuilder<InterviewDrillDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new InterviewDrillDbContext(options);
    }

    private static string GetDefaultConnectionString()
    {
        // Intenta leer appsettings.json del proyecto Api (ubicado un nivel arriba)
        var apiDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "InterviewDrill.Api"));
        var settingsPath = Path.Combine(apiDir, "appsettings.json");

        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) &&
                connStrings.TryGetProperty("DefaultConnection", out var defaultConn))
            {
                return defaultConn.GetString()
                    ?? "Server=(localdb)\\mssqllocaldb;Database=InterviewDrill_Testing;Trusted_Connection=True;TrustServerCertificate=True;";
            }
        }

        return "Server=(localdb)\\mssqllocaldb;Database=InterviewDrill_Testing;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
