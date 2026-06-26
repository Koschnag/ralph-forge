using RalphForge.Core;
using RalphForge.Eval;

namespace RalphForge.Tests;

public class SpecRepairLoopTests
{
    [Fact]
    public async Task Converges_when_the_engine_eventually_proposes_an_inductive_invariant()
    {
        // "attempts <= 2" is rejected (Open@2 --fail--> Locked@3 breaks it);
        // "attempts <= 3" is inductive on the safe machine and is accepted.
        var engine = new FakeEngine("attempts <= 2", "attempts <= 3");
        var loop = new SpecRepairLoop(engine);

        var outcome = await loop.RunAsync(Examples.safeLockout, "attempts must never exceed 3");

        Assert.True(outcome.Converged);
        Assert.Equal("attempts <= 3", outcome.Invariant);
        Assert.Equal(2, outcome.History.Count);
        Assert.False(outcome.History[0].Verified);
        Assert.True(outcome.History[1].Verified);
    }

    [Fact]
    public async Task Gives_up_after_max_iterations_when_no_candidate_is_inductive()
    {
        var engine = new FakeEngine("attempts <= 2", "attempts <= 1", "attempts <= 0");
        var loop = new SpecRepairLoop(engine);

        var outcome = await loop.RunAsync(
            Examples.safeLockout, "unsatisfiable on purpose", new LoopOptions(MaxIterations: 3));

        Assert.False(outcome.Converged);
        Assert.Null(outcome.Invariant);
        Assert.Equal(3, outcome.History.Count);
    }
}
