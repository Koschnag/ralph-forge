module RalphForge.Eval.Repair

open RalphForge.Eval.Contract
open RalphForge.Eval.Gate

/// Parse a candidate invariant (DSL text), set it on the contract, and verify it.
/// This is the primitive the spec-repair loop drives: the engine proposes an
/// invariant as text, the gate accepts only if it is inductive. A parse error is
/// surfaced as a SolverError so the loop can feed it back to the engine.
let verifyCandidateInvariant (invariantText: string) (c: Contract) : GateVerdict =
    match ExprParse.parse invariantText with
    | Error msg -> SolverError $"invariant parse error: {msg}"
    | Ok inv -> verifyInductive { c with Invariant = inv }
