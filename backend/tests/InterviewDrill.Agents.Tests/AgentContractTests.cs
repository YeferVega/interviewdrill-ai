using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents.Tests;

/// <summary>
/// Contract sanity tests — verifies agent interfaces are wired correctly.
/// Real agent tests arrive in Phase 1+ with the golden dataset.
/// </summary>
public class AgentContractTests
{
    [Fact]
    public void AgentInterfaces_AreDefinedInCoreAssembly()
    {
        var coreAssembly = typeof(IJdParserAgent).Assembly;

        Assert.NotNull(coreAssembly.GetType("InterviewDrill.Core.Contracts.IJdParserAgent"));
        Assert.NotNull(coreAssembly.GetType("InterviewDrill.Core.Contracts.IInterviewerAgent"));
        Assert.NotNull(coreAssembly.GetType("InterviewDrill.Core.Contracts.IArchitectureEvaluator"));
        Assert.NotNull(coreAssembly.GetType("InterviewDrill.Core.Contracts.ISecurityRigorJudge"));
        Assert.NotNull(coreAssembly.GetType("InterviewDrill.Core.Contracts.IScoringSynthesizer"));
        Assert.NotNull(coreAssembly.GetType("InterviewDrill.Core.Contracts.IStudyGuideGenerator"));
        Assert.NotNull(coreAssembly.GetType("InterviewDrill.Core.Contracts.IQuestionRegistry"));
    }

    [Fact]
    public void JobProfile_RequiresHashForDeduplication()
    {
        var profile = new JobProfile
        {
            Role = "Backend Engineer",
            RequiredSkills = [new WeightedSkill { Name = "RAG", Weight = 0.9 }],
            RawTextHash = "sha256:abc"
        };

        Assert.Equal("sha256:abc", profile.RawTextHash);
        Assert.Single(profile.RequiredSkills);
    }
}
