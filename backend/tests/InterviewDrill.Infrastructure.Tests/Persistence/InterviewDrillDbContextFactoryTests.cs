using System.IO;
using InterviewDrill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InterviewDrill.Infrastructure.Tests.Persistence;

public sealed class InterviewDrillDbContextFactoryTests : IDisposable
{
    private const string EnvVar = "INTERVIEWDRILL_CONNECTION_STRING";

    private readonly string? _originalEnv;
    private readonly string _originalDir;

    public InterviewDrillDbContextFactoryTests()
    {
        _originalEnv = Environment.GetEnvironmentVariable(EnvVar);
        _originalDir = Directory.GetCurrentDirectory();
        Environment.SetEnvironmentVariable(EnvVar, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvVar, _originalEnv);
        try { Directory.SetCurrentDirectory(_originalDir); } catch (IOException) { /* temp dir removed */ }
    }

    [Fact]
    public void CreateDbContext_WithoutEnvVar_AndNoAppSettings_FallsBackToDefaultConnectionString()
    {
        // Point CWD at an isolated temp dir with no ../InterviewDrill.Api/appsettings.json.
        var temp = Directory.CreateTempSubdirectory("idf-").FullName;
        try
        {
            Directory.SetCurrentDirectory(temp);

            var factory = new InterviewDrillDbContextFactory();
            using var context = factory.CreateDbContext(Array.Empty<string>());

            var cs = context.Database.GetConnectionString();
            Assert.NotNull(cs);
            Assert.Contains("InterviewDrill_Testing", cs);
        }
        finally
        {
            Directory.SetCurrentDirectory(_originalDir);
            TryDelete(temp);
        }
    }

    [Fact]
    public void CreateDbContext_WithEnvVar_UsesProvidedConnectionString()
    {
        Environment.SetEnvironmentVariable(EnvVar, "Server=remote;Database=InterviewDrill_Prod;Trusted_Connection=True");

        var factory = new InterviewDrillDbContextFactory();
        using var context = factory.CreateDbContext(Array.Empty<string>());

        var cs = context.Database.GetConnectionString();
        Assert.NotNull(cs);
        Assert.Contains("remote", cs);
        Assert.Contains("InterviewDrill_Prod", cs);
    }

    [Fact]
    public void CreateDbContext_WithoutEnvVar_ReadsDefaultConnectionFromAppSettings()
    {
        // Arrange an isolated CWD so the factory resolves ../InterviewDrill.Api/appsettings.json.
        var root = Directory.CreateTempSubdirectory("idf-app-").FullName;
        try
        {
            var apiDir = Path.Combine(root, "InterviewDrill.Api");
            Directory.CreateDirectory(apiDir);
            File.WriteAllText(
                Path.Combine(apiDir, "appsettings.json"),
                """{ "ConnectionStrings": { "DefaultConnection": "Server=fromfile;Database=FromFileDb;Trusted_Connection=True" } }""");

            var startDir = Path.Combine(root, "start");
            Directory.CreateDirectory(startDir);
            Directory.SetCurrentDirectory(startDir);

            var factory = new InterviewDrillDbContextFactory();
            using var context = factory.CreateDbContext(Array.Empty<string>());

            var cs = context.Database.GetConnectionString();
            Assert.NotNull(cs);
            Assert.Contains("FromFileDb", cs);
        }
        finally
        {
            Directory.SetCurrentDirectory(_originalDir);
            TryDelete(root);
        }
    }

    private static void TryDelete(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch (IOException) { }
    }
}
