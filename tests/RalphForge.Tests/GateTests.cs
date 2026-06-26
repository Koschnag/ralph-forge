using RalphForge.Eval;

namespace RalphForge.Tests;

public class SmtSpikeTests
{
    [Fact]
    public void Z3_cli_solves_satisfiable_constraint()
    {
        Assert.True(Smt.spikeCheckSat());
    }

    [Fact]
    public void Z3_cli_detects_contradiction()
    {
        Assert.True(Smt.spikeCheckUnsat());
    }
}
