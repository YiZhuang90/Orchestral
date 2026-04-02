# PT-104 Runtime Source Harness Plan

**Goal**

Deliver the first universal replay/simulation proof by letting PT-104-like temperature data reach the experiment-plane consumer path without real PT-104 hardware.

**Scope**

- add a small runtime-facing PT-104 source contract
- keep the existing real `Pt104Session` on that contract
- add internal/test-only replay and synthetic PT-104 broadcasters
- prove `FlowReynoldsDerivedStateSession` can consume those sources without hardware
- keep app-shell source selection and device-panel changes out of scope

**Out Of Scope**

- full virtual PT-104 device behavior
- app-shell `Real / Replay / Synthetic` source selection
- monitor-panel UI changes
- multi-device or camera replay

**Current Reality**

- `Pt104Session` already publishes the outputs downstream consumers need:
  - state snapshot
  - diagnostics snapshot
  - latest reading snapshot
  - reading stream
- `FlowReynoldsDerivedStateSession` currently attaches to a concrete `Pt104Session`
- the current PT-104 tests only use a tiny fake driver; there is no reusable replay/synthetic runtime source yet

**Implementation Shape**

1. Introduce a narrow PT-104 runtime-source contract for downstream consumers.
2. Make `Pt104Session` satisfy that contract unchanged in behavior.
3. Add replay and synthetic PT-104 broadcasters for test/internal use.
4. Change derived-state attachment to accept the contract instead of the concrete session type.
5. Add failing tests first for replay and synthetic PT-104 source consumption.
6. Update architecture/execution docs once the code truth exists.

**Verification Target**

- targeted runtime tests for the new PT-104 source behavior
- full solution build
- full solution test run

**Review Requirement**

Yes. This is a shared runtime-foundation slice that changes a consumer seam and adds universal replay/simulation infrastructure.
