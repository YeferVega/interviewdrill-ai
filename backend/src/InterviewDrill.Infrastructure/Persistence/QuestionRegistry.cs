using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Security;
using InterviewDrill.Core.Vectors;
using InterviewDrill.Infrastructure.Persistence.Entities;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed class QuestionRegistry(
    IQuestionRepository questions,
    IEmbeddingGenerator embeddingGenerator,
    IPiiSanitizer piiSanitizer) : IQuestionRegistry
{
    public async Task<bool> WasAlreadyAskedAsync(
        string userId,
        string jdHash,
        string questionText,
        CancellationToken ct = default)
    {
        var sanitizedText = piiSanitizer.Sanitize(questionText).Text;
        var normalizedHash = QuestionHashing.ComputeNormalizedHash(sanitizedText);

        if (await questions.ExistsNormalizedHashAsync(userId, jdHash, normalizedHash, ct))
        {
            return true;
        }

        var embedding = await embeddingGenerator.GenerateAsync(sanitizedText, ct);

        return await questions.ExistsSemanticDuplicateAsync(
            userId,
            jdHash,
            embedding,
            VectorMath.SemanticDuplicateThreshold,
            ct);
    }

    public async Task<bool> RegisterAsync(
        string sessionId,
        string userId,
        string jdHash,
        InterviewQuestion question,
        CancellationToken ct = default)
    {
        var sanitizedText = piiSanitizer.Sanitize(question.Text).Text;
        var normalizedHash = QuestionHashing.ComputeNormalizedHash(sanitizedText);

        if (await questions.ExistsNormalizedHashAsync(userId, jdHash, normalizedHash, ct))
        {
            return false;
        }

        var embedding = await embeddingGenerator.GenerateAsync(sanitizedText, ct);

        if (await questions.ExistsSemanticDuplicateAsync(
                userId,
                jdHash,
                embedding,
                VectorMath.SemanticDuplicateThreshold,
                ct))
        {
            return false;
        }

        await questions.AddAsync(
            new QuestionEntity
            {
                Id = string.IsNullOrWhiteSpace(question.Id) ? Guid.NewGuid().ToString() : question.Id,
                SessionId = sessionId,
                UserId = userId,
                JobDescriptionHash = jdHash,
                NormalizedQuestionHash = normalizedHash,
                Text = sanitizedText,
                Difficulty = question.Difficulty,
                SkillTarget = question.SkillTarget,
                Embedding = embedding.SelectMany(BitConverter.GetBytes).ToArray()
            },
            ct);

        await questions.SaveChangesAsync(ct);
        return true;
    }
}
