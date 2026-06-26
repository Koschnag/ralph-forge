module RalphForge.Eval.Examples

open RalphForge.Eval.Contract

/// Login lockout protocol: three consecutive failures lock the account.
/// The safety invariant (attempts in [0,3]; Locked implies attempts = 3) is
/// inductive — the gate must VERIFY this contract.
let safeLockout: Contract =
    { Name = "lockout-safe"
      States = [ "Open"; "Locked" ]
      Initial = "Open"
      Vars = [ "attempts", TInt ]
      Init = Eq(Var "attempts", IntLit 0)
      Invariant =
        And
            [ Le(IntLit 0, Var "attempts")
              Le(Var "attempts", IntLit 3)
              Implies(AtState "Locked", Eq(Var "attempts", IntLit 3)) ]
      Transitions =
        [ { From = "Open"
            Event = "fail"
            Guard = Lt(Var "attempts", IntLit 2)
            Updates = [ { Var = "attempts"; Value = Add(Var "attempts", IntLit 1) } ]
            To = "Open" }
          { From = "Open"
            Event = "fail"
            Guard = Eq(Var "attempts", IntLit 2)
            Updates = [ { Var = "attempts"; Value = IntLit 3 } ]
            To = "Locked" }
          { From = "Open"
            Event = "success"
            Guard = BoolLit true
            Updates = [ { Var = "attempts"; Value = IntLit 0 } ]
            To = "Open" }
          { From = "Locked"
            Event = "reset"
            Guard = BoolLit true
            Updates = [ { Var = "attempts"; Value = IntLit 0 } ]
            To = "Open" } ] }

/// Same protocol, but the failure transition has no upper guard — `attempts` can
/// exceed 3, so the invariant `attempts <= 3` is NOT inductive. The gate must
/// REJECT this with a counterexample-to-induction (e.g. attempts = 3 -> 4).
let unsafeLockout: Contract =
    { safeLockout with
        Name = "lockout-unsafe"
        Transitions =
          [ { From = "Open"
              Event = "fail"
              Guard = BoolLit true
              Updates = [ { Var = "attempts"; Value = Add(Var "attempts", IntLit 1) } ]
              To = "Open" }
            { From = "Open"
              Event = "success"
              Guard = BoolLit true
              Updates = [ { Var = "attempts"; Value = IntLit 0 } ]
              To = "Open" }
            { From = "Locked"
              Event = "reset"
              Guard = BoolLit true
              Updates = [ { Var = "attempts"; Value = IntLit 0 } ]
              To = "Open" } ] }
