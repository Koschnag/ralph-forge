module RalphForge.Eval.Gate

open RalphForge.Eval.Contract
open RalphForge.Eval.Encode
open RalphForge.Eval.Smt

/// Verdict of the deterministic proof-gate over a contract's safety invariant.
type GateVerdict =
    | Verified // invariant is inductive: initiation + consecution both hold
    | InitiationFailed of model: string // the initial state already violates the invariant
    | ConsecutionFailed of cti: string // a 1-step transition breaks it (counterexample-to-induction)
    | SolverError of reason: string

let private cur = ""
let private nxt = "_next"

let private sortOf =
    function
    | TInt -> "Int"
    | TBool -> "Bool"

let private stateIdx (c: Contract) =
    c.States |> List.mapi (fun i s -> s, i) |> Map.ofList

let private declAll (suffix: string) (c: Contract) =
    $"(declare-const state{suffix} Int)"
    :: (c.Vars |> List.map (fun (n, t) -> $"(declare-const {n}{suffix} {sortOf t})"))

/// One transition as an SMT relation between current ("") and post ("_next") state.
let private transClause (idx: Map<string, int>) (c: Contract) (t: Transition) =
    let updateConjs =
        c.Vars
        |> List.map (fun (n, _) ->
            match t.Updates |> List.tryFind (fun u -> u.Var = n) with
            | Some u -> $"(= {n}{nxt} {renderExpr idx cur u.Value})"
            | None -> $"(= {n}{nxt} {n}{cur})") // frame: unchanged variable

    let parts =
        [ $"(= state{cur} {Map.find t.From idx})"
          renderExpr idx cur t.Guard
          $"(= state{nxt} {Map.find t.To idx})" ]
        @ updateConjs

    "(and " + String.concat " " parts + ")"

let private q (lines: string list) = String.concat "\n" lines + "\n"

/// Prove the contract's invariant is inductive by refutation: assert the negation
/// of each proof obligation and require UNSAT. SAT yields a concrete counterexample.
///   initiation:  Init(s)            => Inv(s)
///   consecution: Inv(s) /\ T(s,s')  => Inv(s')
let verifyInductive (c: Contract) : GateVerdict =
    let idx = stateIdx c
    let z3 = z3Path ()

    let initQuery =
        q (
            declAll cur c
            @ [ $"(assert (and (= state{cur} {Map.find c.Initial idx}) {renderExpr idx cur c.Init}))"
                $"(assert (not {renderExpr idx cur c.Invariant}))"
                "(check-sat)"
                "(get-model)" ]
        )

    match runZ3 z3 initQuery with
    | Unknown r -> SolverError r
    | Sat model -> InitiationFailed model
    | Unsat ->
        let transOr =
            match c.Transitions with
            | [] -> "false"
            | ts -> "(or " + (ts |> List.map (transClause idx c) |> String.concat " ") + ")"

        let consQuery =
            q (
                declAll cur c
                @ declAll nxt c
                @ [ $"(assert {renderExpr idx cur c.Invariant})"
                    $"(assert {transOr})"
                    $"(assert (not {renderExpr idx nxt c.Invariant}))"
                    "(check-sat)"
                    "(get-model)" ]
            )

        match runZ3 z3 consQuery with
        | Unknown r -> SolverError r
        | Sat cti -> ConsecutionFailed cti
        | Unsat -> Verified

let isVerified =
    function
    | Verified -> true
    | _ -> false

let isConsecutionFailed =
    function
    | ConsecutionFailed _ -> true
    | _ -> false
