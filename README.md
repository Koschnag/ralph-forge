# ralph-forge

**AI-native development machinery with a deterministic proof-gate.**

You write *intent + a safety contract*; an AI engine proposes implementations and
spec refinements; a **deterministic SMT gate accepts only what it can prove**.
The gate — not the frontend — is the point: it keeps a stochastic engine honest by
refuting anything that doesn't hold (UNSAT of the negation).

This is `CDD-v0`: the first vertical slice of a larger "Concept-Driven Development"
idea, deliberately scoped to one thing that works end-to-end rather than a broad
platform.

## Why this and not "yet another spec-driven agent tool"

Spec→agent→code tooling is a crowded 2026 market (Kiro, Spec Kit, Cursor, …). None
of them combine a machine-readable spec with a **machine-checked proof** of the
generated artifact. The defensible core is exactly that gate. The hard problem it
attacks is *spec correctness under nondeterminism*: a proof against the wrong spec
is worthless, so the human stays in the loop on the spec while the machine handles
the proof.

## Architecture

```
 intent + contract ──▶ Engine (proposes) ──▶ Gate (disposes) ──▶ verdict
                          │                      │
   RalphForge.Core ───────┘                      └─────── RalphForge.Eval
   (orchestrator / Ralph loop,                   (deterministic proof-gate:
    engine abstraction)                           contract model, SMT encoding,
        │                                         z3 via SMT-LIB2)
   ClaudeCodeEngine  ──▶ wraps `claude -p`        Gate.verifyInductive:
   (subprocess, via CliWrap — not the API)          initiation:  Init ⇒ Inv
                                                     consecution: Inv ∧ T ⇒ Inv′
   RalphForge.Cli  (thin terminal frontend; a central GUI client comes later)
```

- **Engine = Claude Code CLI, wrapped as a subprocess** (`claude -p`), not the
  Anthropic API/SDK. Swappable behind `IEngine`; fakeable for tests.
- **Gate = z3 via the SMT-LIB2 CLI**, not the `Microsoft.Z3` NuGet (that package
  ships no `linux-x64`/`osx-arm64` natives). Every query is inspectable.
- **Domain (v0): state machines / protocols.** Safety is proved by *inductive
  invariant* checking; a failure returns a concrete counterexample-to-induction.

## Status

- [x] Deterministic SMT proof-gate: verifies a safe state machine, rejects an
      unsafe one with a counterexample (`Examples.safeLockout` / `unsafeLockout`).
- [x] Claude Code engine wrapper (subprocess).
- [ ] Spec-repair Ralph loop (engine proposes → gate disposes → retry).
- [ ] Contract text format + parser.
- [ ] Property-test gate for generated implementations.

## Deployment model

Designed to run on a **Linux host** (server / homelab): the loop, the engine, and
the solver all run server-side. The client is thin — a terminal today, a central
GUI later. Build/run target is `.NET 9`; the gate needs the `z3` CLI on `PATH`
(or `RALPH_Z3`), and the engine needs `claude` (or `RALPH_CLAUDE`).

## Usage

```bash
dotnet run --project src/RalphForge.Cli -- verify safe      # → VERIFIED
dotnet run --project src/RalphForge.Cli -- verify unsafe    # → CONSECUTION FAILED (+ counterexample)
dotnet run --project src/RalphForge.Cli -- engine-check     # wraps `claude -p`, prints the reply
dotnet test                                                 # gate + engine self-tests
```

## Development

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) — layout, the dev loop
(`./scripts/dev.sh build|test|verify|loop`), how to extend (new domain, new
operator, swap the engine), and running persistently on the homelab.

## Method

The outer loop follows **Ralph** (Geoffrey Huntley): a fresh agent context per
iteration, git as state, tests/proofs as the oracle, one task per iteration. The
phase pipeline is **Spec-Driven Development** (note: "SDLC AI Chain" is not an
established term). License: MIT.
