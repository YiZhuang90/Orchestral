# AI Sidecar Design Blueprint for Orchestral (EF-05)

## Context

This blueprint is the **EF-05 deliverable**: detailed agent architecture design for Orchestral's AI sidecar. It answers the open questions from `AGENT_ARCHITECTURE_SKETCH.md` and maps the agent's three capability layers (A1/A2/A3) to a concrete implementation using patterns extracted from three reference architectures.

### Grounding Documents
- `docs/development_v2/PROJECT_VISION.md` — Orchestral lowers the boundary of being a super experimentalist from every possible aspect using AI agents
- `docs/development_v2/ROADMAP_V2.md` — Two-half model (Platform + Agent), execution frontier, boundary rules
- `docs/development_v2/AGENT_ARCHITECTURE_SKETCH.md` — A1/A2/A3 layers, knowledge categories, open questions
- `docs/development/AI_INTEGRATION_PLAN.md` — Sidecar position, control boundary, what AI should/shouldn't do early
- `docs/architecture/AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md` — 10-stage device integration workflow (A-J)

### Reference Architectures Studied
- **Claw Code** (Rust) — tool registry, agent loop, hook system, session persistence
- **Semantic Kernel** (C#) — plugin/function system, filters, provider abstraction, ChatHistory
- **Claude Code / Agent SDK** — tool definitions, permission model, MCP, deferred tool loading, hooks

### Key Design Requirements (from user feedback)
1. **Web browsing is a core capability** — the agent must robustly search for manuals, SDKs, hardware specs, protocols, literature, and community experiences
2. **Anti-database (lessons learned)** — failures are knowledge; the agent warns when someone is about to repeat a known mistake
3. **Traceable logs for industrial/scientific standards** — auto-generated logs must support reproducibility for publication and traceability for compliance (design now, implement incrementally)
4. **Knowledge > Chat history** — conversations are raw material; knowledge is the distilled product. The system must actively extract and structure knowledge from interactions.

### Boundary Rules Respected (from Roadmap V2)
- Rule 4: **Agent proposes, human approves** — all generative actions gated
- Rule 10: **Agent design comes after platform stability** — this blueprint is design only; implementation follows EF-01 through EF-04
- Rule 1: **No coding without roadmap position** — this is EF-05
- Rule 8: **Planning and coding don't mix** — this document is the plan

---

## Development Strategy: Build, Use, Refine

**Principle**: Build the minimum agent that's useful, use it to rebuild the turbulence transition experiment, and refine what's missing through real experience. Each phase produces something you actually use — not a spec that waits for perfection.

### Phase 0: Skeleton (get the agent talking)
**Goal**: A chat panel in the WPF app that can answer questions using Claude.
**Build**: ExperimentAssistant + AIChatPanel + basic system prompt with Orchestral context.
**No plugins, no knowledge, no filters.** Just prove the loop works: you type, Claude responds inside the app.
**Test**: "What is the turbulence transition experiment?" — agent answers from system prompt context.

### Phase 1: The Researcher (agent can find information)
**Goal**: The agent can search the web for hardware docs, SDKs, literature.
**Build**: ResearchPlugin (A2R) — `search_web`, `fetch_page`, `search_vendor_docs`.
**Use it for**: Finding PT-104 SDK docs, HuaTeng camera manuals, pipe flow transition literature. Everything you'd normally Google, ask the agent instead.
**Refine**: Which search patterns work? What's missing? Do you need better parsing of vendor pages?

### Phase 2: The Observer (agent can see the lab)
**Goal**: The agent knows what devices are connected and what's happening.
**Build**: ObservationPlugin (A1) — `observe_run_status`, `observe_device_state`, `read_run_manifest`.
**Use it for**: "Is the PT-104 connected? What temperature is channel 1 reading? What was the last run's status?"
**Refine**: What state queries do you actually ask during a real experiment session?

### Phase 3: The Advisor (agent gives useful suggestions)
**Goal**: The agent can guide experiment design and device integration.
**Build**: GuidancePlugin (A2) — `suggest_experiment_design`, `suggest_integration_steps`, `diagnose_issue`.
**Use it for**: Planning the turbulence experiment top-down — "I want to measure transition at Re 2000-4000, what devices do I need, what should the protocol be?"
**Refine**: Are the suggestions grounded enough? Does it need more knowledge context to be useful?

### Phase 4: The Knowledge Keeper (agent remembers)
**Goal**: The agent maintains a knowledge wiki that accumulates across sessions.
**Build**: Knowledge wiki (Layer 1-3) + KnowledgePlugin + IngestCycle + basic WikiLinter.
**Use it for**: As you rebuild the turbulence experiment, log decisions ("why did I choose 1 Hz sampling?"), record device quirks ("PT-104 needs 30 min warm-up"), capture failures into the anti-database.
**Refine**: Is the wiki structure right? Do the cascade rules make sense? What knowledge are you missing?

### Phase 5: The Drafter (agent produces artifacts)
**Goal**: The agent drafts experiment definitions, device configs, and reports.
**Build**: GenerationPlugin (A3) + ApprovalFilter + SafetyFilter + AuditFilter.
**Use it for**: "Draft an experiment.yaml for the turbulence transition case" → review → accept/edit → load into runtime.
**Refine**: Are the generated artifacts correct? Does the approval flow feel right? What safety checks are missing?

### Phase 6: Hardening (polish from real use)
**Goal**: Everything works smoothly for the turbulence experiment end-to-end.
**Build**: Session persistence, provider config, lint scheduler, traceability metadata.
**Use it for**: Running the full experiment lifecycle: design → configure devices → run → capture data → log results → generate report.
**Refine**: What broke? What was annoying? What should be automatic that isn't?

### The Turbulence Experiment as Acceptance Test

The turbulence transition experiment is the **reference case** (Rule 5 from Roadmap V2). By the end of Phase 5, you should be able to:

1. Ask the agent to help design the experiment (Phase 3)
2. Agent finds relevant literature and device docs (Phase 1)
3. Agent observes what hardware is connected (Phase 2)
4. Agent drafts experiment.yaml with PT-104 + cameras + Reynolds number control (Phase 5)
5. You approve, runtime loads and executes
6. Agent captures decisions, device quirks, and results into the knowledge wiki (Phase 4)
7. Agent drafts a report from the run data (Phase 5)

**If you can do this end-to-end, the agent works.** Everything after that is refinement.

---

## Part 1: Agent Role Mapping

### The Two Halves (from Roadmap V2)

```
ORCHESTRAL = PLATFORM + AGENT

Platform (L1-L4): runs experiments
Agent (A1-A3):    helps build, operate, and learn from experiments
```

### Agent Capability Layers to Plugins and Filters

| Layer | Role (from vision docs) | Maps to SK Component |
|-------|------------------------|---------------------|
| **A1: Observation & Knowledge** | Observe runtime events, capture decisions, track calibration, maintain knowledge base, auto-collect metadata | `ObservationPlugin` — read-only tools that query platform state. `KnowledgePlugin` — read/write to knowledge foundation |
| **A2: Guidance** | Guide device integration, assist experiment design top-down, assist troubleshooting, provide accumulated device knowledge | `GuidancePlugin` — tools that access integration specs, suggest next steps, query knowledge base. `IFunctionInvocationFilter` for context injection |
| **A2R: Research** | Search for manuals, SDKs, protocol docs, literature, community experiences for hardware integration and experiment design | `ResearchPlugin` — web search, web fetch, document parsing. **Core capability**, not optional |
| **A3: Generation** | Draft experiment definitions, generate reports, generate analysis, generate compliance docs | `GenerationPlugin` — tools that produce YAML artifacts, reports, analysis. `ApprovalFilter` gates ALL generative output |

### What the Agent Does at Each Layer

**A1 — Observation (read-only, no approval needed)**
- `observe_run_status()` — current run state, active sessions, stop conditions
- `observe_device_state(deviceId)` — connection status, current readings, errors
- `read_decision_log(date)` — retrieve past decisions and rationale
- `read_calibration_record(deviceId)` — last calibration date, drift, linked runs
- `search_knowledge_base(query)` — search protocols, best practices, device experience
- `read_run_manifest(runId)` — metadata, device list, parameters, outcomes

**A2 — Guidance (read-only + suggestions, no direct action)**
- `suggest_integration_steps(deviceDescription)` — follows the 10-stage workflow (A-J) from AI_GUIDED_HARDWARE_INTEGRATION_SPEC
- `suggest_experiment_design(intent)` — top-down design guidance following Principle 1
- `diagnose_issue(symptoms)` — text-based troubleshooting using device experience knowledge
- `explain_device_protocol(deviceType)` — surface accumulated protocol knowledge
- `suggest_stop_conditions(experimentType)` — recommend safety conditions based on past experiments
- `check_anti_database(intent)` — **before any major action**, search the anti-database for known failures that match the intent. If a match is found, warn the user with the failure context and what to avoid.

**A2R — Research (web-based information gathering, core capability)**
- `search_web(query)` — general web search for manuals, SDKs, specs, community solutions
- `fetch_page(url)` — retrieve and parse a specific web page (vendor docs, datasheets, API references)
- `search_literature(query)` — search academic databases (Google Scholar, arXiv, publisher sites) for relevant papers on experimental methods, device behavior, fluid dynamics
- `search_vendor_docs(deviceType, topic)` — targeted search combining device manufacturer + topic (e.g., "Pico Technology PT-104 SDK Linux")
- `search_community(deviceType, issue)` — search forums, Stack Overflow, GitHub issues for community experiences with specific hardware

**Why A2R is core**: The 10-stage device integration workflow (stages B, C) requires discovering protocol/driver information. Without web access, the agent can only work with what's already in the knowledge base — making it unable to help with *new* devices or *unfamiliar* problems. This is the difference between a useful assistant and a lookup table.

**A3 — Generation (creates artifacts, ALL gated by human approval)**
- `draft_experiment(intent)` produces experiment.yaml — **requires Accept/Edit/Reject**
- `draft_report(runId)` produces experiment report — **requires human review**
- `draft_device_definition(deviceDescription)` produces device.yaml — **requires Accept/Edit/Reject**
- `log_decision(context, decision, rationale)` writes to knowledge foundation — **requires confirmation**
- `draft_analysis(runId, question)` produces analysis output — **requires review**
- `log_failure(context, whatFailed, rootCause, impact, prevention)` writes to anti-database — **requires confirmation**

### The Control Boundary (from AI_INTEGRATION_PLAN)

```
Agent CAN:     suggest, draft, observe, search, explain
Human CAN:     approve, reject, edit, override
Runtime CAN:   execute approved artifacts
Safety CAN:    stop anything at any time
Agent CANNOT:  send live hardware commands, bypass validation, own stop system,
               rewrite runtime behavior, make silent changes to critical logic
```

---

## Part 2: Pattern Application

### Pattern 1 — Tool Registry (from Claw Code to SK Plugins)

**Claw Code pattern**: `GlobalToolRegistry` with 3 tiers (built-in, plugin, runtime) + permission level per tool.

**Applied to Orchestral**: Plugins organized by agent layer, with permission classification:

```csharp
// A1 tools — ReadOnly, no approval needed
public class ObservationPlugin
{
    [KernelFunction("observe_run_status")]
    [Description("Returns the current experiment run state including active device sessions, "
        + "elapsed time, and stop condition status. Use this to understand what's happening "
        + "before making suggestions. Does not modify any state.")]
    public async Task<RunStatusSummary> ObserveRunStatusAsync(CancellationToken ct)
    {
        return await _runtime.GetStatusAsync(ct);
    }

    [KernelFunction("read_calibration_record")]
    [Description("Retrieves the calibration history for a specific device, including last "
        + "calibration date, measured drift, and which experiment runs used this calibration. "
        + "Essential for assessing measurement reliability.")]
    public async Task<CalibrationRecord> ReadCalibrationRecordAsync(
        [Description("The device ID to look up calibration for")] string deviceId,
        CancellationToken ct)
    {
        return await _knowledge.GetCalibrationAsync(deviceId, ct);
    }
}

// A3 tools — Generative, ALL require human approval
public class GenerationPlugin
{
    [KernelFunction("draft_experiment")]
    [Description("Generates an experiment.yaml definition from a natural-language intent. "
        + "The output is a YAML string that must be reviewed and approved by the user "
        + "before it can be loaded into the runtime. Always use observe_ and suggest_ "
        + "tools first to understand the current state before drafting.")]
    public async Task<string> DraftExperimentAsync(
        [Description("Natural language description of what the experiment should do")] string intent,
        [Description("List of device IDs to include")] string[] deviceIds,
        CancellationToken ct)
    {
        // Build YAML from intent + device capabilities + knowledge base
        return await _generator.DraftExperimentYamlAsync(intent, deviceIds, ct);
    }
}
```

### Pattern 2 — Agent Loop (from all three to SK ChatCompletionAgent)

**Universal pattern**: receive → prompt → LLM → parse → tools → loop.

**Applied to Orchestral**: The SK agent loop handles this automatically. Our customization is in the system prompt, which injects the Orchestral domain context:

```csharp
public class ExperimentAssistant
{
    public ExperimentAssistant(Kernel kernel, IKnowledgeFoundation knowledge)
    {
        _agent = new ChatCompletionAgent
        {
            Name = "OrchestralAssistant",
            Instructions = BuildSystemPrompt(knowledge),
            Kernel = kernel
        };
    }

    private string BuildSystemPrompt(IKnowledgeFoundation knowledge)
    {
        return $"""
        You are the Orchestral experiment assistant. Your role is to help the user
        build, operate, and learn from experiments. You are a sidecar — you observe
        and suggest, you never act without approval.

        ## Your Capabilities (use tools in this order)
        1. OBSERVE first — understand current state before suggesting anything
        2. GUIDE — suggest approaches based on knowledge and past experience
        3. GENERATE last — only draft artifacts after the user confirms direction

        ## The 12 Principles You Follow
        1. Top-down experiment design before device details
        2. AI-guided device integration (follow the 10-stage workflow)
        3. Structured data and version management
        4. Automatic metadata logging
        5. Decision logging — always ask why, not just what
        6. Auto-drafted reports with human review gate
        7. Share knowledge across the lab
        8. Reproducibility as first-class guarantee
        9. Calibration tracking per device per run
        10. Compliance and audit trail
        11. Text-first, AI-friendly architecture
        12. Extensible by design

        ## Current Lab State
        Connected devices: {GetDeviceSummary()}
        Active run: {GetRunStatus()}
        Recent decisions: {GetRecentDecisions()}

        ## Control Rules
        - You PROPOSE, the user APPROVES, the runtime EXECUTES
        - Never suggest bypassing validation or safety
        - If uncertain, ask — don't guess
        - Cite knowledge sources when drawing on past experience
        """;
    }
}
```

### Pattern 3 — Safety Hooks (from Claw Code pre-hooks + SK filters)

**Claw Code pattern**: Shell-based pre-hooks with deny/allow/modify exit codes, fail-fast semantics.

**Applied to Orchestral**: Three SK filters enforcing the control boundary:

```csharp
// Filter 1: Safety — block inappropriate tool use
public class SafetyFilter : IFunctionInvocationFilter
{
    // During an active run: only A1 (observation) tools allowed
    // A2/A3 tools blocked — don't distract from a live experiment
}

// Filter 2: Approval — gate all A3 (generative) output
public class ApprovalFilter : IAutoFunctionInvocationFilter
{
    // After any draft_* tool returns, pause the agent loop
    // Surface the draft to the UI: Accept / Edit / Reject
    // On reject: terminate with "user rejected, ask what to change"
    // On accept: allow the artifact to be written
}

// Filter 3: Audit — log everything for compliance (Principle 10)
public class AuditFilter : IFunctionInvocationFilter
{
    // Log every tool call with: timestamp, tool name, inputs, outputs, user context
    // Feeds into the knowledge foundation's decision log
}
```

### Pattern 4 — The Knowledge Wiki (Karpathy's LLM Wiki Pattern + Orchestral Knowledge Foundation)

**Foundational reference**: Karpathy's "LLM Wiki" essay — knowledge management systems fail because humans find maintenance tedious, not because reading/thinking is hard. The solution: **delegate bookkeeping to the AI, humans focus on curation and direction.**

**Core principle: Knowledge > Chat History**

Conversations are raw material. Knowledge is the distilled product. The system actively maintains an **interconnected wiki** where one new piece of information cascades through 10-15 related pages — not isolated summaries.

#### The Three-Layer Architecture

```
LAYER 3 — SCHEMA (governance)
  ORCHESTRAL_KNOWLEDGE_SCHEMA.md
  Defines: ingest rules, cascade rules, lint rules, human gates
  "When new experiment result arrives, update: Results, Methods,
   Common_Failures (if issue), Active_Experiments summary"

LAYER 2 — KNOWLEDGE WIKI (evolving synthesis)
  Interconnected markdown pages, continuously updated
  The agent maintains this; humans curate and verify
  Categories K1-K10 are organizational structure, not silos
  Pages cross-reference each other via wiki links

LAYER 1 — IMMUTABLE SOURCES (raw material)
  Sensor data exports, chat transcripts, calibration logs,
  imported PDFs/papers, device manuals, raw experiment logs
  Never modified — only ingested into Layer 2

EPHEMERAL — Chat history
  Compacted and eventually discarded
  Knowledge is extracted before compaction
```

#### Layer 2: The Knowledge Wiki Structure

```
knowledge/
├── ORCHESTRAL_KNOWLEDGE_SCHEMA.md          # Layer 3: governance rules
├── experiments/                             # K1: Project Context (Why/How/What)
│   ├── turbulence-transition-2026/
│   │   ├── _summary.md                     # Status, blockers, next steps
│   │   ├── _methods.md                     # Procedures tested, with links to K3
│   │   ├── _results.md                     # Data, interpretations, anomalies (K6)
│   │   ├── _issues.md                      # Problems encountered, links to K7
│   │   └── _data_manifest.md               # Where raw data lives (K5)
│   └── [next-experiment]/
├── decisions/                               # K2: Decisions with rationale
│   ├── 2026-04-03-pt104-channel-config.md  # Links to experiment + device pages
│   └── 2026-04-05-camera-resolution.md
├── techniques/                              # K3: Protocols & Best Practices
│   ├── pt104-warmup-protocol.md            # Links to experiments that use it
│   ├── camera-calibration-checklist.md
│   └── reynolds-number-control.md
├── devices/                                 # K4: Device Info Packages
│   ├── pt104/
│   │   ├── _overview.md                    # SDK, protocol, capabilities
│   │   ├── _known_issues.md               # Links to K7 failures
│   │   └── _calibration_history.md        # K8: per-device calibration
│   └── huateng-camera/
├── anti-database/                           # K7: Failures & Lessons Learned
│   ├── usb-hub-data-gaps.md               # Links to affected experiments
│   ├── temperature-drift-false-positive.md
│   └── _patterns.md                        # Common failure patterns index
├── literature/                              # K9: Papers, manuals, specs
│   ├── avila-2011-transition-threshold.md  # Links to experiments referencing it
│   ├── pico-pt104-sdk-manual.md
│   └── _reading-list.md                   # Queued & annotated
├── safety/                                  # K10: Incidents & near-misses
│   ├── 2026-03-20-laser-interlock.md
│   └── _safety-checklist.md
└── raw/                                     # Layer 1: immutable sources
    ├── imported-papers/
    ├── chat-transcripts/
    ├── calibration-exports/
    └── device-manuals/
```

**Key insight from Karpathy: Pages are interconnected, not isolated.** When a new experiment result arrives, the ingest cycle touches:
- The experiment's `_results.md`
- The `techniques/` page for the method used (mark as validated or flag issues)
- The `anti-database/` if a known failure pattern appeared
- The `devices/` page for any device-specific observations
- The experiment's `_data_manifest.md` with storage locations
- The `decisions/` log if parameters were changed mid-run

#### The Ingest Cycle (knowledge distillation)

```
Source arrives (chat, experiment data, paper, observation)
         |
         v
  1. CLASSIFY — what type of source?
     Chat -> extract decisions, failures
     Experiment data -> results, methods
     Paper -> literature, techniques
     Observation -> device info, safety
         |
         v
  2. IDENTIFY — what pages need updating?
     Schema rules define cascade:
     "New result -> touch 5-10 pages"
     "New failure -> touch anti-db +
      affected experiment + device page"
         |
         v
  3. DRAFT — agent writes/updates pages
     Non-destructive: new info doesn't
     overwrite old — it's synthesized
     Contradictions flagged, not hidden
         |
         v
  4. HUMAN GATE — user reviews changes
     Accept / Edit / Reject per page
     Agent proposes, human approves
         |
         v
  5. COMMIT — approved changes persisted
     Provenance metadata attached
     Cross-references updated
     Audit log entry created
```

#### Token Budget & Progressive Disclosure (L0-L3)

Don't load all knowledge into context. Use progressive expansion:

```
L0 (Hot):   Current experiment summary + recent decisions         ~2k tokens
L1 (Warm):  Related techniques + device pages                    ~4k tokens
L2 (Cool):  Anti-database matches + literature references         ~4k tokens
L3 (Cold):  Historical experiments + full calibration records     ~8k tokens

Query: "What's the status?" -> Load L0 only
Query: "Why did run 42 fail?" -> Load L0 + L1 + search L2 (anti-db)
Query: "New postdoc — teach them calibration" -> Load L1 + L2
Query: "Compare our results with published data" -> Load L0 + L2 (literature)
```

#### Lint Operations (periodic health checks)

The agent should periodically (weekly or on-demand) run health checks:

| Lint Rule | Action | Example |
|-----------|--------|---------|
| **Stale experiments** | Flag experiments with no updates > 2 weeks | "Turbulence project hasn't been updated since March 20" |
| **Contradictions** | Detect conflicting claims across pages | "Technique A says warm-up is 30 min; experiment 5 used 15 min" |
| **Orphaned pages** | Find technique/device pages not referenced by any experiment | "Camera lens cleaning protocol exists but no experiment references it" |
| **Expiring calibrations** | Flag devices approaching calibration due dates | "PT-104 S/N 12345 calibration expires in 2 weeks" |
| **Unresolved anti-db entries** | Failures without prevention measures documented | "USB hub data gap — no mitigation recorded" |
| **Missing cross-references** | Pages that should link to each other but don't | "Decision log references experiment 7 but experiment 7 has no link back" |

#### Knowledge Categories (K1-K10)

These are **organizational structure within the wiki**, not separate databases:

| # | Category | Scope | Content | Example |
|---|----------|-------|---------|---------|
| K1 | **Project Context (Why/How/What)** | Per-project | Why this experiment exists, how it's designed, what it measures | "This project studies laminar-turbulent transition in pipe flow at Re 2000-4000" |
| K2 | **Decisions Log** | Per-project, date-indexed | What was decided, why, what alternatives were rejected | "Chose PT-104 over thermocouple DAQ because 0.001C resolution needed for boundary layer detection" |
| K3 | **Protocols & Best Practices** | Per-device-type, versioned, shareable | Step-by-step procedures that worked | "PT-104 warm-up protocol: power on 30 min before measurement; first 5 readings are drift" |
| K4 | **Device Info Package** | Per-device-model | Everything needed to reuse a device in a new project | "HuaTeng camera: SDK v3.2, P/Invoke wrapper, 640x480 max at 200fps, needs USB3, overheats above 45C ambient" |
| K5 | **Data Log** | Per-run, linked to artifacts | Where raw data lives, what format, how to access | "Run 2026-04-03-001: Parquet files in /data/runs/20260403/, 4 PT-104 channels at 1Hz, 2h duration" |
| K6 | **Experiment Results** | Per-run | Outcomes, observations, anomalies, key findings | "Transition observed at Re=2350 +/- 50, consistent with literature. Unexpected oscillation in channel 3 after 45 min" |
| K7 | **Anti-Database (Failures)** | Lab-wide, cross-project | What went wrong, root cause, impact, how to detect, how to prevent | "USB hub caused PT-104 data gaps — never share USB bus between camera and sensor" |
| K8 | **Calibration Records** | Per-device, linked to runs | Last calibration date, drift measurements, validity period | "PT-104 S/N 12345: calibrated 2026-03-15, drift <0.002C/month, next due 2026-09-15" |
| K9 | **Literature & References** | Per-topic | Papers, manuals, specs linked to experiments and devices | "Avila et al. (2011) Science — transition threshold Re=2040 for pipe flow, used as baseline" |
| K10 | **Safety & Incident Log** | Lab-wide | Near-misses, equipment damage, protocol violations, lessons | "Laser trigger fired without interlock check — added mandatory pre-run safety checklist" |

#### Provenance Metadata (traceable by design)

Every wiki page and every update carries:

```yaml
# Page-level provenance (front-matter)
---
page_id: K2-2026-04-03-001
category: decisions
created: 2026-04-03T14:32:00Z
last_updated: 2026-04-05T09:15:00Z
update_history:
  - date: 2026-04-03T14:32:00Z
    by: agent (Claude Sonnet 4, session abc123)
    approved_by: Yi Zhuang
    action: created
    source: conversation
  - date: 2026-04-05T09:15:00Z
    by: agent (Claude Sonnet 4, session def456)
    approved_by: Yi Zhuang
    action: updated (added calibration link)
    source: observation
linked_experiments: [turbulence-transition-2026]
linked_devices: [pt104-sn12345]
linked_decisions: []
linked_anti_db: []
linked_literature: [avila-2011]
---
```

This format supports:
- **Scientific reproducibility**: Another researcher traces exactly what was decided and why
- **Publication data provenance**: "All data collected under agent-logged conditions with traceable calibration records"
- **Industrial compliance**: Append-only audit trail with timestamps and approvals
- **Knowledge archaeology**: When a team member leaves, the wiki preserves not just what they knew, but why

#### The Schema Document (Layer 3)

`ORCHESTRAL_KNOWLEDGE_SCHEMA.md` governs all wiki operations:

```markdown
# Knowledge Schema

## Ingest Rules
- New experiment result -> update: experiment/_results.md, techniques/[method].md,
  anti-database/ (if issue), devices/[used]/known_issues.md
- New decision -> update: decisions/[date]-[topic].md, experiment/_summary.md
- New failure -> update: anti-database/[pattern].md, experiment/_issues.md,
  devices/[affected]/known_issues.md
- New paper ingested -> update: literature/[citation].md, link to relevant
  experiments and techniques

## Contradiction Rules
- When new information contradicts an existing page:
  - Do NOT overwrite — add a "Contradiction" section
  - Flag for human review with both claims cited
  - Only resolve after human approval

## Human Gates
- All new pages: require approval
- All updates to techniques/ and safety/: require approval
- Anti-database entries: require confirmation
- Literature annotations: auto-approved (low risk)
- Cross-reference links: auto-approved (bookkeeping)

## Lint Schedule
- Weekly: stale experiments, expiring calibrations, unresolved anti-db
- Monthly: orphaned pages, missing cross-references, contradiction scan
```

### Pattern 5 — Provider Abstraction (from Claw Code enum dispatch + SK connectors)

```csharp
// One-line provider swap — no tool code changes
var builder = Kernel.CreateBuilder();

// Today: Claude
builder.AddAnthropicChatCompletion("claude-sonnet-4-20250514", apiKey);

// Or tomorrow: local model for offline lab use
// builder.AddOllamaChatCompletion("llama3", new Uri("http://localhost:11434"));
```

---

## Part 3: Answering the Open Questions (from AGENT_ARCHITECTURE_SKETCH)

| Question | Answer |
|----------|--------|
| **Build custom vs. extend Claude Code/Codex vs. hybrid?** | **Hybrid**: Use Semantic Kernel as the framework (not Claude Code CLI). Claude Code remains the developer tool for building Orchestral itself. The in-app agent is a separate SK-based assistant. |
| **How does knowledge survive sessions?** | **File-based initially** — 10 knowledge categories (K1-K10) stored as YAML files in a `knowledge/` directory within the project, one subdirectory per category. Each entry carries provenance metadata (who, when, why, linked artifacts). Database-backed later when volume justifies it. Mirrors the artifact-first philosophy. Chat history is ephemeral; knowledge is the permanent distillation. |
| **Multi-user architecture?** | **Deferred** — single-user first (one experimentalist per station). Knowledge foundation is file-based and can be shared via git. Multi-user comes in Horizon F. |
| **Observation interface (push vs. pull)?** | **Pull initially** — agent queries platform state via A1 tools when asked. Push (event subscriptions) added later when A1 needs to proactively surface anomalies. |
| **Artifact handoff format?** | **YAML strings** — agent drafts YAML, human approves, runtime loads. Same format as existing experiment/device/protocol artifacts. No new format needed. |
| **Dev-to-production agent relationship?** | Claude Code = dev agent (builds the platform). Orchestral Agent = production agent (helps run experiments). They share the text-first philosophy but are separate systems. |

---

## Part 4: What Needs to Be Built

### New Project: ExperimentalControlPlatform.AI

| File | Layer | Purpose | Est. lines |
|------|-------|---------|------------|
| `ExperimentAssistant.cs` | Core | Agent setup, system prompt builder, session management, knowledge distillation trigger | ~220 |
| `Plugins/ObservationPlugin.cs` | A1 | Read-only tools for runtime state, devices, knowledge queries | ~150 |
| `Plugins/GuidancePlugin.cs` | A2 | Suggestion tools for integration, design, troubleshooting, anti-database checks | ~220 |
| `Plugins/ResearchPlugin.cs` | A2R | Web search, page fetch, literature search, vendor doc search, community search | ~200 |
| `Plugins/GenerationPlugin.cs` | A3 | Draft tools for experiments, reports, analysis, failure logging | ~200 |
| `Plugins/KnowledgePlugin.cs` | A1+A3 | Read/write to knowledge foundation (10 categories K1-K10) | ~200 |
| `Filters/SafetyFilter.cs` | Guard | Block inappropriate tools during active runs | ~60 |
| `Filters/ApprovalFilter.cs` | Guard | Gate all A3 output through human review | ~80 |
| `Filters/AuditFilter.cs` | Guard | Log all tool calls with provenance metadata for compliance | ~80 |
| `Filters/AntiDatabaseFilter.cs` | Guard | Before A3 actions, auto-check anti-database for matching failures | ~60 |
| `Knowledge/IKnowledgeWiki.cs` | Data | Interface for the three-layer knowledge wiki (read, write, search, lint) | ~80 |
| `Knowledge/FileKnowledgeWiki.cs` | Data | File-based wiki implementation (markdown with YAML front-matter provenance) | ~300 |
| `Knowledge/IngestCycle.cs` | Data | Multi-step knowledge distillation: classify, identify pages, draft, gate, commit | ~200 |
| `Knowledge/WikiLinter.cs` | Data | Periodic health checks: stale pages, contradictions, orphans, expiring calibrations | ~150 |
| `Knowledge/KnowledgeSchema.cs` | Data | Loads and enforces ORCHESTRAL_KNOWLEDGE_SCHEMA.md cascade/lint/gate rules | ~100 |

### New Files in ExperimentalControlPlatform.App

| File | Purpose | Est. lines |
|------|---------|------------|
| `AIChatPanel.xaml` | Chat UI — messages, input, approval buttons | ~120 |
| `AIChatPanelViewModel.cs` | Binds panel to ExperimentAssistant | ~140 |
| `App.xaml` DataTemplate entry | Map ViewModel to View | ~3 |
| `App.xaml.cs` DI wiring | Wire AI services into composition root | ~15 |

### NuGet Dependencies

| Package | Purpose |
|---------|---------|
| `Microsoft.SemanticKernel` | Core agent framework |
| `Microsoft.SemanticKernel.Connectors.Anthropic` | Claude provider |

### Unchanged (Reused As-Is)

- All device services, RuntimeCoordinator, artifact models
- `ObservableObject`, `Dispatcher.InvokeAsync` pattern
- Theme system, DevicePanelShell
- YAML artifact format and validation

### Total: ~2,580 lines of new C# across ~18 files + knowledge wiki structure

---

## Part 5: Implementation Order (maps to Development Strategy phases)

**Prerequisites** (EF-01 through EF-04 should progress in parallel — Rule 10):
- EF-01: PT-104 runtime harness landed (done)
- EF-02: Experiment data skeleton defined
- EF-03: Experiment-logic code boundary established
- EF-04: First reference experiment working

**Phase 0 through 1 can begin immediately** (agent doesn't need platform stability to chat and research). Later phases integrate as EF-02 through EF-04 land.

| Step | Phase | What to build | Deliverable |
|------|-------|---------------|-------------|
| 1 | P0 | Add SK NuGet packages + ExperimentAssistant + AIChatPanel | Agent talks inside the app |
| 2 | P1 | ResearchPlugin (A2R) — web search, page fetch, vendor docs, literature | Agent can find hardware docs and papers |
| 3 | P2 | ObservationPlugin (A1) — device state, run status, manifests | Agent sees the lab |
| 4 | P3 | GuidancePlugin (A2) — experiment design, integration steps, troubleshooting | Agent gives advice |
| 5 | P4 | Knowledge wiki structure + `IKnowledgeWiki` + `FileKnowledgeWiki` + `KnowledgeSchema` | Three-layer wiki exists |
| 6 | P4 | KnowledgePlugin + IngestCycle — distill from conversations into wiki | Agent remembers across sessions |
| 7 | P4 | WikiLinter — stale pages, contradictions, expiring calibrations | Wiki stays healthy |
| 8 | P5 | GenerationPlugin (A3) — draft experiments, reports, device configs | Agent drafts artifacts |
| 9 | P5 | ApprovalFilter — human gate on all A3 output | Accept/Edit/Reject workflow |
| 10 | P5 | SafetyFilter + AuditFilter + AntiDatabaseFilter | Guardrails in place |
| 11 | P6 | Session persistence, provider config, traceability metadata polish | Production-ready |
| 12 | P6 | End-to-end turbulence experiment acceptance test | Full lifecycle validated |

---

## Part 6: Verification

1. **Build**: `dotnet build platform/ExperimentalControlPlatform.sln`
2. **A1 test**: "What devices are connected?" — ObservationPlugin returns live state
3. **A2 test**: "How should I set up a temperature sweep?" — GuidancePlugin suggests design based on knowledge wiki
4. **A2R test**: "Find the SDK documentation for Pico PT-104" — ResearchPlugin searches web, returns relevant links and content
5. **A3 test**: "Draft an experiment for Reynolds number 2300" — GenerationPlugin produces YAML, ApprovalFilter surfaces to UI, Accept, artifact written
6. **Anti-database test**: Log a failure ("USB hub caused data gaps"), later ask to set up PT-104 on a USB hub — AntiDatabaseFilter fires warning
7. **Safety test**: During active run, ask to modify experiment — SafetyFilter blocks
8. **Ingest cycle test**: After a conversation with a decision, trigger ingest — verify it touches multiple wiki pages (decision log + experiment summary + device page), human gate for each, pages committed with provenance
9. **Cascade test**: Log a new experiment result — verify it updates `_results.md`, the relevant `techniques/` page, and `_data_manifest.md` per schema rules
10. **Contradiction test**: Ingest information that contradicts an existing technique page — verify agent flags it for review instead of overwriting
11. **Lint test**: Run WikiLinter — verify it detects a stale experiment (no updates in 2 weeks) and an expiring calibration
12. **Provenance test**: Every wiki page has YAML front-matter with: page_id, created, last_updated, update_history (who, when, action, source, approved_by), linked cross-references
13. **Progressive disclosure test**: Simple query loads only L0 (~2k tokens); complex diagnostic query progressively expands to L1+L2
14. **Knowledge persistence test**: Write knowledge, restart app, query — knowledge persists with full provenance and cross-references intact

---

## Part 7: How This Maps Back to the Vision

> "Orchestral lowers the boundary of being a super experimentalist from every possible aspect using AI agents."

| Barrier (from PROJECT_VISION) | Agent Feature That Removes It |
|-------------------------------|-------------------------------|
| Hardware knowledge barrier | A2: `suggest_integration_steps()` + **A2R: `search_vendor_docs()`, `search_community()`** — agent finds manuals, SDKs, and community solutions on the web |
| No top-down design guidance | A2: `suggest_experiment_design()` + A3: `draft_experiment()` |
| No data/version management | A1: `read_run_manifest()` + knowledge foundation (K5: Data Log) |
| No automatic metadata | A1: `observe_device_state()` auto-captures during runs + traceable provenance on all entries |
| No decision logging | A3: `log_decision()` with rationale capture (K2) + `KnowledgeDistiller` extracts decisions from conversations |
| No automatic reporting | A3: `draft_report()` with human review gate |
| No lab knowledge sharing | A1: `search_knowledge_base()` across K1-K10 — includes anti-database (K7), protocols (K3), device packages (K4), literature (K9) |
| Repeating past mistakes | **K7 Anti-Database** + `AntiDatabaseFilter` — agent automatically warns when a known failure pattern is about to be repeated |
| Untraceable data | **Provenance metadata** on every knowledge entry and audit log — supports publication data provenance and industrial compliance |
| Knowledge lost when people leave | **10 knowledge categories** distilled from conversations, persisted as structured YAML, shareable across the lab |
| Graphical code hostile to AI | Text-first YAML architecture — agent reads/writes natively |
