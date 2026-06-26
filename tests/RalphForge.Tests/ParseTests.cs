using RalphForge.Eval;

namespace RalphForge.Tests;

public class ParseTests
{
    [Fact]
    public void Parses_and_verifies_the_safe_invariant()
        => Assert.True(Gate.isVerified(
            Repair.verifyCandidateInvariant(
                "0 <= attempts and attempts <= 3 and (@Locked -> attempts = 3)",
                Examples.safeLockout)));

    [Fact]
    public void A_too_weak_invariant_is_rejected_on_the_unsafe_machine()
        => Assert.False(Gate.isVerified(
            Repair.verifyCandidateInvariant("attempts <= 3", Examples.unsafeLockout)));

    [Fact]
    public void Garbage_invariant_does_not_verify()
        => Assert.False(Gate.isVerified(
            Repair.verifyCandidateInvariant("this is (not valid", Examples.safeLockout)));
}
