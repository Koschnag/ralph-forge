using RalphForge.Eval;

namespace RalphForge.Tests;

public class SmtSmokeTests
{
    [Fact]
    public void Z3_cli_solves_satisfiable_constraint() => Assert.True(Smt.spikeCheckSat());

    [Fact]
    public void Z3_cli_detects_contradiction() => Assert.True(Smt.spikeCheckUnsat());
}

public class GateTests
{
    [Fact]
    public void Gate_verifies_safe_inductive_contract()
        => Assert.True(Gate.isVerified(Gate.verifyInductive(Examples.safeLockout)));

    [Fact]
    public void Gate_rejects_non_inductive_contract()
        => Assert.True(Gate.isConsecutionFailed(Gate.verifyInductive(Examples.unsafeLockout)));
}
