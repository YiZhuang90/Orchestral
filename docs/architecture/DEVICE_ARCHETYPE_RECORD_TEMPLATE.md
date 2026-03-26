# Device Archetype Record Template

## Purpose

This template records how one integrated device was mapped into the Orchestral panel and runtime architecture.

Use one record per integrated device.

Suggested location:

- `docs/devices/<device-name>/device-archetype-record.md`

## Device

- device name:
- model:
- transport:
- identity format:

## Chosen Archetype

- primary archetype:
- secondary archetype, if any:
- why this mapping was selected:

## Chosen Widget Stack

- mandatory widgets:
- optional widgets included:
- optional widgets deferred:

## Default Parameter Surface

- default visible parameters:
- advanced settings moved out of the default surface:
- why:

## Ownership Model

- device-scope state:
- endpoint-scope state:
- session-scope state:

## Runtime IO Model

- acquisition / controlled / hybrid:
- command path:
- data path:
- hybrid decision rule, if relevant:

## Panel Output Model

- data output:
- status output:
- diagnostics output:
- applied-settings output:
- session-end output:

## Timing Model

- PC timestamp source:
- hardware timing source, if any:
- timing notes:

## Open Decisions

- unresolved architecture questions:
- deferred work:

## Evidence

- official documentation used:
- hardware verification performed:
- test harnesses used:
