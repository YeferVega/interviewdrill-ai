using InterviewDrill.Infrastructure.Persistence;
using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InterviewDrill.Infrastructure.Tests.Persistence;

public sealed class InterviewDrillDbContextModelTests
{
    [Fact]
    public void Embedding_IsMappedToVarBinaryColumn()
    {
        using var db = CreateContext();

        var property = db.Model
            .FindEntityType(typeof(QuestionEntity))!
            .FindProperty(nameof(QuestionEntity.Embedding))!;

        Assert.Equal("varbinary(max)", property.GetColumnType());
    }

    [Fact]
    public void Embedding_HasNoVectorIndexOnSqlServerExpress()
    {
        using var db = CreateContext();

        var entityType = db.Model.FindEntityType(typeof(QuestionEntity))!;
        var property = entityType.FindProperty(nameof(QuestionEntity.Embedding))!;

        // SQL Server Express no soporta índices vectoriales; el índice de Embedding
        // debe estar ausente para evitar fallos de migración en este proveedor.
        var index = entityType.GetIndexes().FirstOrDefault(i => i.Properties.Contains(property));
        Assert.Null(index);
    }

    [Fact]
    public void InterviewSession_HasStateJsonColumn()
    {
        using var db = CreateContext();

        var property = db.Model
            .FindEntityType(typeof(InterviewSessionEntity))!
            .FindProperty(nameof(InterviewSessionEntity.InterviewerStateJson))!;

        Assert.True(property.IsNullable);
    }

    [Fact]
    public void InitialCreateMigrationExistsInInfrastructureAssembly()
    {
        var hasInitialCreate = typeof(InterviewDrillDbContext).Assembly
            .GetTypes()
            .Any(t => t.Name.EndsWith("InitialCreate", StringComparison.Ordinal));

        Assert.True(hasInitialCreate);
    }

    private static InterviewDrillDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InterviewDrillDbContext>()
            .UseSqlServer("Server=localhost;Database=InterviewDrillTests;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new InterviewDrillDbContext(options);
    }
}
