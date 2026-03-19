# Product Thesis And Wedge

## 1. Purpose

This document defines the product thesis, first market wedge, and early validation logic for Orchestral.

It exists to keep the project from drifting into abstract platform-building without a sharp product direction.

## 2. Product Identity

**Orchestral — Design, Run, and Evolve Experiments with AI**

Category:

**AI-native experimental operating system**

## 3. Core Product Thesis

Scientists should be able to design experiments at the system level first, then use AI to turn device manuals, protocol details, and experiment intent into a runnable, documented, reproducible experimental system.

The system should not only help with runtime control.

It should also help with:

- experiment design,
- hardware and protocol integration,
- orchestration,
- monitoring,
- documentation,
- structured run and data management,
- experiment evolution over time.

## 4. Why This Product Should Exist

Existing workflows, especially LabVIEW-centered ones, still leave major burdens in place.

The key problem is:

**LabVIEW provides useful tools for coding, hardware integration, and data handling, but it does not naturally guide users toward layered experiment design, strong documentation, or structured experimental knowledge. For many users, the quality of system design, data organization, and experiment records still depends heavily on personal discipline.**

That gap creates room for a new product category.

## 5. Product Promise

The practical product promise of Orchestral is:

1. define the experiment at a high level,
2. provide device manuals or technical references,
3. specify what each device should provide,
4. let AI help generate the integration scaffolding and system structure,
5. validate before execution,
6. run with monitoring, stop logic, documentation, and structured outputs,
7. evolve the experiment later without rebuilding everything from scratch.

## 6. First Market Wedge

The first wedge is not "all experimental control."

The first wedge is:

- custom academic and research experiments,
- mixed hardware environments,
- single-computer orchestration,
- experiments that change often,
- scientists who understand the science and device needs but do not want to hand-build software infrastructure repeatedly.

This wedge is large enough to matter and narrow enough to execute.

## 7. Ideal Early User

The ideal early user is someone who:

- runs custom experiments rather than buying a fixed commercial instrument,
- needs to integrate multiple devices,
- changes experiment logic often,
- is capable enough to understand the experiment deeply,
- but does not want to spend large amounts of time building and maintaining custom control software.

This includes users very much like the current project owner.

## 8. First Two Validation Experiments

The first validation path should be:

1. rebuild the turbulence-transition experiment in Orchestral,
2. build the planned flame experiment using the same architecture,
3. identify what abstractions actually survive both cases,
4. generalize only where repeated reuse proves the abstraction is real.

This is the right way to test whether Orchestral is truly becoming a platform rather than just a one-off rebuild.

## 9. Initial Product Boundary

In the near term, Orchestral should be understood as:

- a real product for building and operating custom experiments,
- validated through concrete experiments,
- with platform ambitions,
- but not yet trying to be universally generalized before the wedge is proven.

This means:

- product first,
- platform second,
- but architecture designed so the product can grow into a platform.

## 10. Main Moat

The moat should not be "AI" alone.

The stronger moat is the combination of:

- protocol-aware hardware modeling,
- reusable device integration patterns,
- structured experiment artifacts,
- runtime safety and automatic stop logic,
- generated documentation and design memory,
- structured run and data management,
- and AI as the accelerator across all of these layers.

## 11. Main Risks

The biggest early risks are:

- building too much abstract platform infrastructure before real validation,
- making AI promises that are too magical internally,
- generalizing before turbulence and flame prove the architecture,
- building for contributors or architecture taste before building for early users.

## 12. Near-Term Success Criteria

Orchestral should be considered on the right path if it can:

1. rebuild the existing turbulence experiment cleanly,
2. support the flame experiment without architectural collapse,
3. add a new device faster and more cleanly than the old workflow,
4. generate and preserve better documentation and run structure by default,
5. make experiment evolution materially easier than in the current workflow.

## 13. Final Statement

Orchestral is not just a control program and not just a framework.

It is intended to become an AI-native experimental operating system, starting with a sharp wedge in custom research experiments where hardware integration, high-level system design, documentation, and structured data management are all painful enough that existing tools are not good enough.
