using RalphForge.Core;
using RalphForge.Eval;

if (args.Length == 0)
{
    Console.WriteLine(
        """
        ralph — CDD-v0  (engine: Claude Code CLI · gate: z3/SMT)
        Commands:
          verify <safe|unsafe>     run the SMT proof-gate on an example contract
          loop                     spec-repair loop: claude proposes an inductive invariant, gate verifies
          engine-check [prompt]    wrap `claude -p` as a subprocess and print the reply
        """);
    return 0;
}

switch (args[0])
{
    case "verify":
    {
        var which = args.Length > 1 ? args[1] : "safe";
        var contract = which == "unsafe" ? Examples.unsafeLockout : Examples.safeLockout;
        var verdict = Gate.verifyInductive(contract);
        Console.WriteLine($"contract: {contract.Name}");
        Console.WriteLine(Gate.describe(verdict));
        return Gate.isVerified(verdict) ? 0 : 1;
    }
    case "engine-check":
    {
        var prompt = args.Length > 1 ? string.Join(' ', args[1..]) : "Reply with exactly one word: PONG";
        var engine = new ClaudeCodeEngine();
        var res = await engine.RunAsync(new EngineRequest(prompt, Timeout: TimeSpan.FromMinutes(2)));
        Console.WriteLine($"exit={res.ExitCode}");
        Console.WriteLine(res.Success ? res.Output : $"ERROR: {res.Error}");
        return res.Success ? 0 : 1;
    }
    case "loop":
    {
        var contract = Examples.safeLockout;
        var intent =
            "After 3 consecutive failures the account locks; attempts must never exceed 3, "
            + "and in the Locked state attempts equals 3.";
        var loop = new SpecRepairLoop(new ClaudeCodeEngine());
        var outcome = await loop.RunAsync(contract, intent, new LoopOptions(MaxIterations: 6));
        foreach (var step in outcome.History)
            Console.WriteLine($"[iter {step.Iteration}] {(step.Verified ? "OK " : "no ")}{step.Candidate}");
        Console.WriteLine(outcome.Converged
            ? $"\nCONVERGED -> inductive invariant:\n  {outcome.Invariant}"
            : "\nDID NOT CONVERGE within the iteration budget");
        return outcome.Converged ? 0 : 1;
    }
    default:
        Console.Error.WriteLine($"unknown command: {args[0]}");
        return 1;
}
