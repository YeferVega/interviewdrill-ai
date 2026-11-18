using InterviewDrill.Core.Vectors;
using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed class SqlQuestionRepository(InterviewDrillDbContext context) : IQuestionRepository
{
    public Task<bool> ExistsNormalizedHashAsync(
        string userId,
        string jobDescriptionHash,
        string normalizedQuestionHash,
        CancellationToken ct = default)
    {
        return context.Questions.AsNoTracking().AnyAsync(
            q => q.UserId == userId
                && q.JobDescriptionHash == jobDescriptionHash
                && q.NormalizedQuestionHash == normalizedQuestionHash,
            ct);
    }

    public Task<bool> ExistsSemanticDuplicateAsync(
        string userId,
        string jobDescriptionHash,
        float[] embedding,
        double similarityThreshold = VectorMath.SemanticDuplicateThreshold,
        CancellationToken ct = default)
    {
        if (embedding.Length == 0)
            throw new ArgumentException("Embedding vector must not be empty.", nameof(embedding));

        if (similarityThreshold is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(similarityThreshold), similarityThreshold, "Similarity threshold must be between 0 and 1.");

        // SQL Server Express no soporta índices vectoriales ni VectorDistance,
        // por lo tanto la búsqueda semántica se resuelve en memoria sobre los
        // candidatos del mismo usuario y JobDescriptionHash.
        var candidates = context.Questions.AsNoTracking()
            .Where(q => q.UserId == userId && q.JobDescriptionHash == jobDescriptionHash)
            .Where(q => q.Embedding != null)
            .Select(q => new { q.Id, Embedding = q.Embedding! })
            .ToList();

        foreach (var candidate in candidates)
        {
            var stored = BytesToFloatArray(candidate.Embedding);
            if (VectorMath.CosineSimilarity(embedding, stored) >= similarityThreshold)
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    private static float[] BytesToFloatArray(byte[] bytes)
    {
        var floats = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    public Task AddAsync(QuestionEntity question, CancellationToken ct = default)
    {
        return context.Questions.AddAsync(question, ct).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return context.SaveChangesAsync(ct);
    }
}
