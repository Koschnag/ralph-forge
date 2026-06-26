module RalphForge.Eval.Encode

open RalphForge.Eval.Contract

/// Render an expression to SMT-LIB2. `suffix` selects the variable copy:
/// "" for current-state variables, "_next" for post-state (primed) variables.
/// `stateIdx` maps control-state names to integer ids; the control variable is
/// rendered as "state<suffix>".
let rec private render (stateIdx: Map<string, int>) (suffix: string) (e: Expr) : string =
    let r = render stateIdx suffix

    match e with
    | IntLit n -> if n < 0 then $"(- {-n})" else string n
    | BoolLit b -> if b then "true" else "false"
    | Var v -> v + suffix
    | AtState s ->
        match Map.tryFind s stateIdx with
        | Some i -> $"(= state{suffix} {i})"
        | None -> failwithf "unknown state in expression: %s" s
    | Add(a, b) -> $"(+ {r a} {r b})"
    | Sub(a, b) -> $"(- {r a} {r b})"
    | Eq(a, b) -> $"(= {r a} {r b})"
    | Lt(a, b) -> $"(< {r a} {r b})"
    | Le(a, b) -> $"(<= {r a} {r b})"
    | Not a -> $"(not {r a})"
    | And [] -> "true"
    | And xs -> "(and " + (xs |> List.map r |> String.concat " ") + ")"
    | Or [] -> "false"
    | Or xs -> "(or " + (xs |> List.map r |> String.concat " ") + ")"
    | Implies(a, b) -> $"(=> {r a} {r b})"

let renderExpr (stateIdx: Map<string, int>) (suffix: string) (e: Expr) : string = render stateIdx suffix e
