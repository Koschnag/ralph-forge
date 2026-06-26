module RalphForge.Eval.Render

open RalphForge.Eval.Contract

/// Render an expression back to the contract DSL (infix), e.g. "attempts <= 3".
let rec exprToDsl (e: Expr) : string =
    match e with
    | IntLit n -> string n
    | BoolLit b -> if b then "true" else "false"
    | Var v -> v
    | AtState s -> "@" + s
    | Add(a, b) -> $"({exprToDsl a} + {exprToDsl b})"
    | Sub(a, b) -> $"({exprToDsl a} - {exprToDsl b})"
    | Eq(a, b) -> $"({exprToDsl a} = {exprToDsl b})"
    | Lt(a, b) -> $"({exprToDsl a} < {exprToDsl b})"
    | Le(a, b) -> $"({exprToDsl a} <= {exprToDsl b})"
    | Not a -> $"(not {exprToDsl a})"
    | And xs -> "(" + (xs |> List.map exprToDsl |> String.concat " and ") + ")"
    | Or xs -> "(" + (xs |> List.map exprToDsl |> String.concat " or ") + ")"
    | Implies(a, b) -> $"({exprToDsl a} -> {exprToDsl b})"

/// Render the contract as a readable spec block for the engine prompt.
let contractForPrompt (c: Contract) : string =
    let vars =
        c.Vars
        |> List.map (fun (n, t) ->
            let ts =
                match t with
                | TInt -> "int"
                | TBool -> "bool"

            $"{n} : {ts}")
        |> String.concat ", "

    let trans =
        c.Transitions
        |> List.map (fun t ->
            let upd =
                t.Updates
                |> List.map (fun u -> $"{u.Var} := {exprToDsl u.Value}")
                |> String.concat ", "

            $"  {t.From} --{t.Event} [{exprToDsl t.Guard}]--> {t.To} {{ {upd} }}")
        |> String.concat "\n"

    let states = String.concat ", " c.States

    $"States: {states}\nInitial: {c.Initial}\nVars: {vars}\nInit: {exprToDsl c.Init}\nTransitions:\n{trans}"
