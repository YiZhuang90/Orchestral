# LabVIEW Replacement Thesis

## 1. Purpose

This document clarifies the strategic product thesis behind the project.

The long-term ambition is not only to build a better control system for one turbulence experiment. The long-term ambition is to build an AI-native alternative to NI LabVIEW for custom experimental control.

## 2. Core Thesis

LabVIEW provides useful tools for coding, hardware integration, and data handling, but it does not solve the full burden of building and operating modern experimental systems.

More specifically:

- it does not remove the need for hardware and protocol understanding,
- it does not naturally guide users toward layered experiment design,
- it often pulls users into implementation detail early enough that systems grow organically,
- it does not naturally produce strong documentation, design memory, and structured experimental knowledge,
- the quality of data organization, logs, and experiment records still depends heavily on the user.

This project exists to address those gaps.

## 3. Main Critique of LabVIEW

### 3.1 Cost

LabVIEW is expensive.

That cost is not only financial. It also includes lock-in around tooling, workflow, and ecosystem habits.

### 3.2 Reduced coding is not reduced integration burden

LabVIEW reduces some low-level coding burden through graphical construction and available integration tooling.

But the user still needs to understand:

- what device they are talking to,
- how that device communicates,
- what protocol family is involved,
- what parameters matter,
- what timing constraints exist,
- what output contract the device should satisfy.

In practice, this means LabVIEW reduces some low-level effort without removing the broader integration burden from the user.

### 3.3 High-level system design is weakened

A more serious problem is that LabVIEW does not naturally scaffold a layered experiment-design process and often pushes users into implementation detail early.

When a new component is added, the user quickly has to deal with:

- concrete connections,
- detailed interactions,
- low-level flow decisions,
- immediate compatibility concerns.

That makes it harder for many users to design the experiment first at the system level.

Instead of encouraging a layered process:

1. define the experiment,
2. define component roles,
3. prepare each device,
4. define orchestration,
5. run the system,

it often leads users toward direct organic construction.

This is workable in the short term, but it tends to produce systems that grow by local fixes rather than by deliberate architecture.

### 3.4 Documentation and data management remain weak points

For real labs, documentation and structured data management are not optional.

Labs need:

- reusable protocol records,
- experiment logs,
- decision records,
- run manifests,
- structured outputs,
- repeatable data interpretation,
- preserved operational knowledge.

LabVIEW offers data-handling and reporting tools, but it is not designed around these broader lab-level knowledge artifacts as first-class outputs of the experiment-building process.

## 4. Replacement Thesis

The replacement thesis is:

Scientists should be able to design an experiment at the system level first, then let AI help turn device manuals, protocol details, and experiment intent into a runnable, documented, reproducible experimental system.

The system should do this through:

- a high-level experiment design layer,
- protocol-aware device preparation,
- structured orchestration artifacts,
- runtime-owned safety and stop logic,
- schema-driven UI generation,
- continuous documentation generation,
- structured run and data management.

## 5. Product Positioning

The project should be positioned as:

**An AI-native, layered alternative to LabVIEW for custom research experiments.**

More specifically, the initial wedge is:

- custom academic and research experiments,
- single-computer control systems,
- mixed hardware environments,
- experiments that change often,
- users who understand science and hardware needs but do not want to hand-build software infrastructure every time.

## 6. User Promise

The product promise should be:

1. describe the experiment at a high level,
2. provide device manuals or technical references,
3. define what each device should provide,
4. let AI help generate the integration scaffolding, control structure, UI, and documentation,
5. validate before execution,
6. run with structured monitoring, stop logic, and reproducible outputs.

Externally, this can feel simple.

Internally, the system must still follow a strict rule:

- AI suggests,
- humans approve,
- runtime executes,
- safety can always stop.

## 7. Strategic Wedge

The first market wedge should not be "replace LabVIEW everywhere."

The first wedge should be:

- research environments with custom hardware combinations,
- systems that are frequently modified,
- experiments where documentation and data discipline are weak,
- users who want AI assistance with integration and system setup.

This wedge is large enough to matter and narrow enough to execute.

## 8. Validation Path

The project should validate this thesis through real experiments, not only architecture.

Current planned path:

1. rebuild the turbulence-transition experiment using the new system,
2. build the planned flame experiment using the same system,
3. use those two cases to test what is genuinely reusable,
4. generalize only where repeated reuse proves it is justified.

This is the correct path for avoiding fake generality.

## 9. Moat

The moat should not be "AI" alone.

The stronger moat is the combination of:

- protocol-aware hardware modeling,
- reusable device integration patterns,
- structured experiment artifacts,
- runtime safety and automatic stop logic,
- schema-driven UI generation,
- generated documentation and design memory,
- reproducible run and data management,
- AI as the accelerator across all of the above.

## 10. Final Strategic Statement

This project is not just an internal cleanup of one experiment-control codebase.

It is an attempt to build a layered, AI-native replacement for LabVIEW in the part of the market where experiments are custom, hardware is mixed, requirements change often, and existing tools do not adequately handle integration burden, system-level design, documentation, or structured data management.
