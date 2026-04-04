# Landing Audit — 2026-04-03

## Purpose

This audit classifies every recent Orchestral slice and branch against real Git evidence, so that the waitlist reflects truth instead of assumption.

Requested by PL-002 in [CROSS_THREAD_DEV_WAITLIST.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/CROSS_THREAD_DEV_WAITLIST.md).

---

## Classification Logic

| Classification | Rule |
|---------------|------|
| **Landed** | All commits reachable from `origin/main` |
| **Ready To Land** | Implemented, reviewed, verified on a branch; not yet merged to `origin/main` |
| **In Progress** | Active work exists but slice is not done |
| **Blocked** | Explicit blocker prevents continuation or landing |
| **Historical** | Old branch/worktree whose work is already incorporated or carries no unique content |

---

## Git Evidence

Audit date: 2026-04-03

- `origin/main` HEAD: `be0529f` (docs: clarify canvas block boundaries)
- Local `main` HEAD: `af36d25` (docs: add runtime io and timing architecture)
- Remote: `https://github.com/YiZhuang90/Orchestral.git`

### origin/main commit chain (30 commits, newest first)

```
be0529f docs: clarify canvas block boundaries
6d0c36c docs: define top-level experiment function classes
cf48ca9 docs: define experiment canvas block contract
ad0bf19 docs: define experiment canvas and virtual twin architecture
c80c8a1 feat: add experiment-definition linting
ecee4c0 feat: add cross-session validation
101d926 feat: decompose control center runtime capabilities
16339a4 docs: plan universal completion before experiment logic proof
cf71681 docs: define experiment logic layer
7f22a9d feat: add flow reynolds derived-state wiring
3847bd1 feat: add run monitor and alarm surface
e1cd16a feat: add controller unit session
ac8fa68 feat: add run recorder and artifact writer
9eaf021 feat: add run context metadata unit
5e218f8 feat: add buffered high-rate stream delivery
f981eb4 feat: add experiment definition role binding
323c90d feat: add runtime coordinator stop authority
4756f63 feat: add session-local runtime validation
154783e feat: add safe panel close and apply-exit lifecycle
f314cad feat: add runtime operator controls
83a5331 feat: complete runtime io section migrations
f50a777 feat: add control-center runtime session pilot
07ce629 feat: add microphone runtime session foundation
01e4162 docs: add runtime io and timing architecture
caf3262 Migrate Orchestral panels to the v2 design system (#1)
779fa4d docs: consolidate architecture references
00da8e4 feat: add reusable device panels and hardware integrations
5c9de89 docs: add device panel architecture guides
33238a2 docs: add integration panel contract and io spec
900b04c Merge remote initial commit
```

### Branch divergence from origin/main

| Branch | Commits not on origin/main | Has remote? |
|--------|---------------------------|-------------|
| `codex/pt104-runtime-source-harness` | 1 (`7465325`) | yes |
| `codex/orchestral-design-v2-landing` | 8 | yes |
| `codex/orchestral-design-v2-migration` | 1 | no |
| `codex/orchestral-design-v2-migration-full` | 9 | no |
| `codex/runtime-io-doc-push` | 0 | no |
| `codex/runtime-io-microphone` | 0 | yes |
| `claude/cool-almeida` | 0 | no |
| `codex/panel-contract-enforcement` | 0 | no |

### Worktree inventory

| Path | Branch | Commit |
|------|--------|--------|
| Main checkout | `main` | `af36d25` |
| `…/runtime-io-microphone` | `codex/pt104-runtime-source-harness` | `7465325` |
| `…/design-v2-landing` | `codex/orchestral-design-v2-landing` | `4d59b86` |
| `…/orchestral-design-v2-migration` | `codex/orchestral-design-v2-migration` | `dbb9d33` |
| `…/panel-contract-enforcement` | `codex/orchestral-design-v2-migration-full` | `5f9b270` |
| `…/runtime-io-doc-push` | `codex/runtime-io-doc-push` | `01e4162` |

### Stash inventory

| Index | Description |
|-------|-------------|
| `stash@{0}` | `On main: codex-temp-before-runtime-io-push` |
| `stash@{1}` | `WIP on main: 779fa4d docs: consolidate architecture references` |

---

## Slice Classification

### Landed

**LD-000: V2 design + runtime foundation + architecture docs baseline**

Verified: all 29 non-initial commits on `origin/main` through `be0529f`.

Three groups:

1. **V2 design migration** — PR #1 (commit `caf3262`). Theme tokens, shared shell, widgets, panel templates.
2. **Runtime foundation chain** — commits `01e4162` through `c80c8a1`. Microphone session, control-center pilot, runtime IO migrations, operator controls, safe panel close, session-local validation, coordinator + stop authority, experiment definition + role binding, high-rate stream buffering, run context, run recorder, controller unit session, run monitor + alarm surface, flow/Reynolds derived-state, control-center capability decomposition, cross-session validation, experiment-definition linting.
3. **Architecture docs** — commits `cf71681` through `be0529f`. Experiment logic layer, canvas architecture, canvas block contract, typed block kinds, virtual twin strategy, universal completion planning.

### Ready To Land

**LD-001: PT-104 runtime-source harness**

- Branch: `codex/pt104-runtime-source-harness`
- Commit: `7465325` (1 commit ahead of `origin/main`)
- Evidence: `git log --oneline origin/main..codex/pt104-runtime-source-harness` returns exactly 1 commit
- Review and verification artifacts exist in the `runtime-io-microphone` worktree
- Needs: the merge into `origin/main`

### Planned But Not Ready

**SL-001: Minimum experiment-logic proof using Flow/Reynolds**

- No branch, no code
- Blocked on PL-001 (roadmap re-baseline must confirm this is the right next step)

### Needs Planning

**PL-001: Strategic roadmap re-baseline and ROADMAP_V2**

- Not started

### Historical Branches

| Branch | Evidence | Reason |
|--------|----------|--------|
| `codex/orchestral-design-v2-landing` | 8 commits diverged, but PR #1 squash-merged equivalent content to origin/main | Work incorporated via PR #1 |
| `codex/orchestral-design-v2-migration` | 1 commit diverged; earlier draft of the same migration | Superseded by PR #1 |
| `codex/orchestral-design-v2-migration-full` | 9 commits diverged; fuller draft of same migration | Superseded by PR #1 |
| `codex/runtime-io-doc-push` | 0 commits diverged | Fully merged |
| `codex/runtime-io-microphone` | 0 commits diverged | Fully merged |
| `claude/cool-almeida` | 0 commits diverged | No unique work |
| `codex/panel-contract-enforcement` | 0 commits diverged | No unique work |

---

## Operational Findings

### Stale local main

Local `main` is at `af36d25` (8 commits). `origin/main` is at `be0529f` (29 commits). Local main diverged before the runtime work was pushed through worktrees to GitHub.

The commit `af36d25` (docs: add runtime io and timing architecture) on local main appears to be a duplicate of `01e4162` on `origin/main` — same content, created independently.

Two stashes also exist from before the runtime IO push.

**Impact:** before doing any new coding work in the main checkout folder, local main needs to be synced with `origin/main`. The worktrees are more current and are not affected.

### Worktree cleanup opportunity

5 worktrees exist beyond the main checkout. Only the `runtime-io-microphone` worktree is actively useful (it hosts the Ready To Land PT-104 slice). The other 4 are for historical branches and could be cleaned up whenever convenient.

---

## Summary

| State | Count | Items |
|-------|-------|-------|
| **Landed** | 29 commits | V2 design, runtime foundation, architecture docs |
| **Ready To Land** | 1 slice | PT-104 runtime-source harness |
| **Planned But Not Ready** | 1 item | Experiment-logic proof (blocked on PL-001) |
| **Needs Planning** | 1 item | Roadmap re-baseline (PL-001) |
| **Ready For Coding** | 0 | Intentionally empty until PL-001 completes |
| **Historical** | 7 branches | Old drafts/merged branches, recorded for reference |
