# Development

## Where it runs

RalphForge runs on the **codespace** homelab host (the Mac is a thin client). The
host has `.NET 9`, the `z3` CLI, `claude` (authenticated), and `gh`. Edit via
VS Code Remote-SSH to codespace, or any editor + git.

```bash
ssh codespace                       # or the `cs` alias
cd ~/ralph-forge
```

## Quick start

```bash
./scripts/dev.sh build              # compile
./scripts/dev.sh test               # gate + parser + loop tests (expects all green)
./scripts/dev.sh verify             # SMT gate on the safe example  → VERIFIED
./scripts/dev.sh verify unsafe      # the failing example           → counterexample
./scripts/dev.sh loop               # spec-repair loop: claude proposes, z3 disposes
./scripts/dev.sh engine-check       # smoke-test the claude wrapper → PONG
```

Raw equivalents: `dotnet test RalphForge.slnx`,
`dotnet run --project src/RalphForge.Cli -- <command>`.

## Layout

```
src/RalphForge.Eval/   (F#)  — the SPOT + the gate (the moat)
  Contract.fs   the model = Single Point of Truth (states, vars, transitions, invariant)
  ExprParse.fs  DSL text  -> Expr        (FParsec)
  Encode.fs     Expr      -> SMT-LIB2
  Smt.fs        z3 CLI runner (SMT-LIB2 over stdin)
  Gate.fs       verifyInductive: initiation + consecution, by refutation
  Render.fs     Expr/contract -> DSL / prompt text
  Examples.fs   sample contracts
  Repair.fs     parse a candidate invariant + gate it
src/RalphForge.Core/   (C#)  — orchestration
  Engine.cs            IEngine abstraction (loop + GUI depend only on this)
  ClaudeCodeEngine.cs  wraps `claude -p` via CliWrap (subprocess, not the API)
  FakeEngine.cs        deterministic engine for tests
  SpecRepairLoop.cs    the Ralph loop (engine proposes -> gate disposes -> retry)
src/RalphForge.Cli/    (C#)  — verify / loop / engine-check
tests/RalphForge.Tests/ (xunit)
```

## The gate (the moat)

A contract's safety invariant is proved **inductive** by refutation (z3):

- initiation:  `Init ∧ ¬Inv`        must be **UNSAT**
- consecution: `Inv ∧ T ∧ ¬Inv'`    must be **UNSAT** (SAT ⇒ counterexample-to-induction)

AI output is accepted only when z3 returns UNSAT. A rejected proposal yields a
concrete counterexample, which the loop feeds into the next attempt.

## Extending

### Add an example contract / domain
Write a `Contract` value (see `Examples.fs`): states, initial, vars, init,
invariant, transitions. Check it with `Gate.verifyInductive yourContract`.

### Add an expression operator
One responsibility per file — touch four:
1. `Contract.fs`  — add the `Expr` case
2. `Encode.fs`    — render it to SMT-LIB2
3. `ExprParse.fs` — parse it (FParsec operator/atom)
4. `Render.fs`    — render it back to the DSL

Then add a test in `ParseTests.cs` / `GateTests.cs`.

### Swap or fake the engine
Implement `IEngine`. The loop and CLI depend only on the interface; `FakeEngine`
is the test pattern (deterministic, no network/auth).

## Run persistently on the homelab (survives Mac disconnect)

```bash
ssh codespace -t "tmux send-keys -t ralph './scripts/loop.sh' Enter"
ssh codespace -t "tmux attach -t ralph"        # Ctrl-b d to detach
```

## Auth

The engine uses your Claude subscription via `claude` on codespace. On a 401:

```bash
ssh codespace -t "claude auth login"           # then: claude auth status
```

## Formatting

C#: `./scripts/dev.sh fmt` (`dotnet format`). F#: Ionide/Fantomas in the editor.
`.editorconfig` pins indent/EOL/charset for both.
