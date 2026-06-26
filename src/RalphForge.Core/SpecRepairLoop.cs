using RalphForge.Eval;
using EvalContract = RalphForge.Eval.Contract.Contract;

namespace RalphForge.Core;

public sealed record LoopOptions(int MaxIterations = 8);

public sealed record LoopStep(int Iteration, string Candidate, bool Verified, string Verdict);

public sealed record LoopOutcome(bool Converged, string? Invariant, IReadOnlyList<LoopStep> History);

/// The Ralph loop, specialised for spec-repair: the engine proposes a safety
/// invariant, the deterministic gate accepts only if it is inductive, and each
/// rejection (with its counterexample) is fed back into the next attempt. The
/// engine proposes; the gate disposes.
public sealed class SpecRepairLoop
{
    private readonly IEngine _engine;

    public SpecRepairLoop(IEngine engine) => _engine = engine;

    public async Task<LoopOutcome> RunAsync(
        EvalContract contract,
        string intent,
        LoopOptions? options = null,
        CancellationToken ct = default)
    {
        var opts = options ?? new LoopOptions();
        var history = new List<LoopStep>();
        string? lastFailure = null;

        for (var i = 1; i <= opts.MaxIterations; i++)
        {
            ct.ThrowIfCancellationRequested();

            var prompt = BuildPrompt(contract, intent, lastFailure);
            var reply = await _engine.RunAsync(
                new EngineRequest(prompt, Timeout: TimeSpan.FromMinutes(3)), ct);

            if (!reply.Success)
            {
                history.Add(new LoopStep(i, "", false, $"engine error: {reply.Error}"));
                lastFailure = $"the engine failed: {reply.Error}";
                continue;
            }

            var candidate = CleanInvariant(reply.Output);
            var verdict = Repair.verifyCandidateInvariant(candidate, contract);
            var verified = Gate.isVerified(verdict);
            var verdictText = Gate.describe(verdict);

            history.Add(new LoopStep(i, candidate, verified, verdictText));

            if (verified)
                return new LoopOutcome(true, candidate, history);

            lastFailure = $"Candidate: {candidate}\n{verdictText}";
        }

        return new LoopOutcome(false, null, history);
    }

    private static string BuildPrompt(EvalContract contract, string intent, string? lastFailure)
    {
        var machine = Render.contractForPrompt(contract);
        var failure = lastFailure is null
            ? ""
            : $"\nYour previous attempt was REJECTED by the proof-gate:\n{lastFailure}\nPropose a STRONGER invariant that fixes this.\n";

        return $"""
            You propose a SAFETY INVARIANT for a state machine. It must be INDUCTIVE:
            it holds in the initial state and is preserved by every transition. A solver
            (z3) verifies it and rejects anything that is not inductive.

            Intent (the safety goal): {intent}

            State machine:
            {machine}

            Invariant grammar: comparisons (= < <=), logic (and or not ->), arithmetic (+ -),
            @State for a control-state test, bare names for variables, integer literals.
            Example: 0 <= attempts and attempts <= 3 and (@Locked -> attempts = 3)
            {failure}
            Output ONLY the invariant expression. No prose, no code fences.
            """;
    }

    /// Strip Markdown fences/backticks an engine might wrap the expression in.
    private static string CleanInvariant(string raw)
    {
        var s = raw.Trim();

        if (s.StartsWith("```"))
        {
            var lines = s.Split('\n').Where(l => !l.TrimStart().StartsWith("```"));
            s = string.Join('\n', lines).Trim();
        }

        return s.Trim('`').Trim();
    }
}
