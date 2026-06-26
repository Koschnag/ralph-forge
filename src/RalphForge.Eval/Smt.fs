module RalphForge.Eval.Smt

open System
open System.Diagnostics

/// Verdict from the SMT solver on a single SMT-LIB2 query.
type SmtResult =
    | Sat of model: string
    | Unsat
    | Unknown of reason: string

/// z3 executable: RALPH_Z3 env override, else resolved from PATH as "z3".
///
/// We shell out to the z3 CLI (SMT-LIB2 over stdin) instead of the Microsoft.Z3
/// NuGet binding: that package (latest 4.12.2) ships only win-x64/osx-x64 natives
/// — no linux-x64 (codespace) and no osx-arm64 (dev Mac). The CLI is cross-platform,
/// newer (4.15+), and keeps every query inspectable: we always prove a safety
/// property by asserting UNSAT of its negation.
let z3Path () =
    match Environment.GetEnvironmentVariable "RALPH_Z3" with
    | null | "" -> "z3"
    | p -> p

/// Run an SMT-LIB2 query through `z3 -smt2 -in` (stdin) and classify the verdict.
let runZ3 (exe: string) (smt2: string) : SmtResult =
    let psi = ProcessStartInfo(exe, "-smt2 -in")
    psi.RedirectStandardInput <- true
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.UseShellExecute <- false
    match Process.Start psi with
    | null -> Unknown "z3: process could not be started"
    | proc ->
        use p = proc
        p.StandardInput.Write smt2
        p.StandardInput.Close()
        let out = p.StandardOutput.ReadToEnd()
        let err = p.StandardError.ReadToEnd()
        p.WaitForExit()
        let trimmed = out.Trim()
        let first =
            trimmed.Split('\n')
            |> Array.tryHead
            |> Option.map (fun s -> s.Trim())
            |> Option.defaultValue ""
        match first with
        | "unsat" -> Unsat
        | "sat" -> Sat trimmed
        | "unknown" -> Unknown(if String.IsNullOrWhiteSpace err then trimmed else err.Trim())
        | other -> Unknown $"unexpected z3 output: '{other}'; stderr: {err.Trim()}"

let isSat =
    function
    | Sat _ -> true
    | _ -> false

let isUnsat =
    function
    | Unsat -> true
    | _ -> false

// --- toolchain spike (throwaway): confirms the z3 CLI is reachable and parsed ---
let private spikeSat = "(declare-const x Int)\n(assert (> x 2))\n(assert (< x 5))\n(check-sat)\n"
let private spikeUnsat = "(declare-const x Int)\n(assert (> x 5))\n(assert (< x 2))\n(check-sat)\n"

let spikeCheckSat () = runZ3 (z3Path ()) spikeSat |> isSat
let spikeCheckUnsat () = runZ3 (z3Path ()) spikeUnsat |> isUnsat
