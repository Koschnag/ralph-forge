module RalphForge.Eval.Contract

/// Type of an extended-state variable.
type VarType =
    | TInt
    | TBool

/// Expression over the extended state (control state + typed variables).
/// `AtState s` is true when the control state equals the named state `s`.
type Expr =
    | IntLit of int
    | BoolLit of bool
    | Var of string
    | AtState of string
    | Add of Expr * Expr
    | Sub of Expr * Expr
    | Eq of Expr * Expr
    | Lt of Expr * Expr
    | Le of Expr * Expr
    | Not of Expr
    | And of Expr list
    | Or of Expr list
    | Implies of Expr * Expr

/// An assignment `Var := Value` performed by a transition.
type Update = { Var: string; Value: Expr }

/// A guarded transition: in state `From`, on `Event`, if `Guard` holds, apply
/// `Updates` and move to `To`. Variables not in `Updates` are unchanged (frame).
type Transition =
    { From: string
      Event: string
      Guard: Expr
      Updates: Update list
      To: string }

/// A state-machine / protocol contract — the machine-readable specification the
/// human writes and the gate verifies (the single source of truth).
type Contract =
    { Name: string
      States: string list
      Initial: string
      Vars: (string * VarType) list
      Init: Expr        // condition on the initial extended state (over variables)
      Invariant: Expr   // safety property that must hold in every reachable configuration
      Transitions: Transition list }
