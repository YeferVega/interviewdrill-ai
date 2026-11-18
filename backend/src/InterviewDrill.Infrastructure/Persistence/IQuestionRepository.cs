using InterviewDrill.Core.Vectors;
using InterviewDrill.Infrastructure.Persistence.Entities;

namespace InterviewDrill.Infrastructure.Persistence;

public interface IQuestionRepository
{
    Task<bool> ExistsNormalizedHashAsync(
        string userId,
        string jobDescriptionHash,
        string normalizedQuestionHash,
        CancellationToken ct = default);

    Task<bool> ExistsSemanticDuplicateAsync(
        string userId,
        string jobDescriptionHash,
        float[] embedding,
        double similarityThreshold = VectorMath.SemanticDuplicateThreshold,
        CancellationToken ct = default);

    Task AddAsync(QuestionEntity question, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
