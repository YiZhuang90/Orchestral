# Open-Core And Commercialization Model

## 1. Purpose

This document defines a working commercialization direction for Orchestral.

The goal is to combine:

- early public development,
- strong technical credibility,
- community adoption where helpful,
- and a realistic path to revenue.

## 2. Strategic Position

Orchestral should not begin as a fully closed product built in isolation.

It also should not blindly open every layer from day one before the product boundary is understood.

The most promising direction is:

**build in public first, then move toward an open-core commercialization model**

This means:

- public vision,
- public architecture,
- public progress,
- likely open infrastructure layers,
- but selective control over the commercially critical product layers until the wedge is proven.

## 3. Why Open-Core Fits This Project

Orchestral targets a research and lab environment.

In this market, users care about:

- transparency,
- trust,
- extensibility,
- local control,
- compatibility with unusual devices,
- the ability to inspect what the system is doing.

That makes a pure black-box strategy less attractive.

At the same time, labs and institutes will still pay for:

- working packaged solutions,
- validated integrations,
- support,
- deployment help,
- maintenance,
- reproducibility and audit features,
- enterprise and institute-ready workflows.

That makes open-core a strong candidate.

## 4. Recommended Layer Split

### 4.1 Likely open layer

The following layers are good candidates to be public and eventually open:

- artifact schemas,
- experiment-definition format,
- protocol model,
- capability model,
- example experiment packages,
- simulator and replay tools,
- some generic device adapters,
- selected local single-machine runtime components,
- public documentation.

These layers support adoption, trust, and community participation.

### 4.2 Likely commercial layer

The following layers are good candidates for commercial packaging:

- institute-ready packaged distribution,
- validated device integration packs,
- premium protocol/device library coverage,
- onboarding and setup support,
- managed updates,
- premium AI workflows,
- collaboration and run-management features,
- audit, governance, and deployment tooling,
- private deployment and institute support contracts.

This is where users are likely to pay, because this is where operational burden is removed.

## 5. Initial Go-To-Market Logic

The first business should not be "sell software licenses to everyone."

The first business should be closer to:

- help labs replace painful custom control workflows,
- package Orchestral into a working local solution,
- support setup and device integration,
- reduce experiment-building burden,
- provide ongoing support and extension.

This means revenue can begin through:

- pilot projects,
- institute deployments,
- integration and setup contracts,
- support agreements,
- premium packaged editions.

## 6. Build-In-Public Strategy

Orchestral should still be built in public.

That means sharing:

- vision,
- architecture,
- progress,
- experiment demos,
- lessons from turbulence and flame,
- selected technical artifacts,
- public thinking around system design and LabVIEW replacement.

This helps:

- attract early users,
- attract technically aligned collaborators,
- build trust,
- sharpen the product through feedback,
- create category visibility early.

## 7. Why Not Open Everything Immediately

Even in a build-in-public model, opening every layer too early carries risk.

The main risk is not code theft. The main risk is losing flexibility before the true product boundary is clear.

Early on, the project is still discovering:

- what part is infrastructure,
- what part is product,
- what part is moat,
- what users will actually pay for.

If every layer is fully open and socially frozen too early, the project can:

- commit to the wrong architecture,
- give away the wrong commercial layer,
- optimize for contributors before customers,
- generalize too early.

So the right path is:

- build publicly,
- open selected layers,
- keep key product layers flexible until the wedge is proven.

## 8. Likely Commercial Offers

Once the wedge is validated, Orchestral could plausibly offer:

- community edition,
- institute edition,
- premium device/protocol packs,
- AI-assisted setup and integration workflows,
- private deployment,
- validation and support contracts,
- training and onboarding,
- experiment-system migration services from legacy workflows.

## 9. Validation Before Monetization Expansion

Before locking the business model, the project should first validate:

1. the turbulence experiment works in the new architecture,
2. the flame experiment works in the same architecture,
3. another lab or user with similar pain wants the solution,
4. that user values packaging, support, and validated integration enough to pay.

Until those are true, the business model should remain directional, not overcommitted.

## 10. Recommended Near-Term Stance

The recommended near-term stance is:

- develop Orchestral in public,
- keep the product thesis public,
- keep the architecture visible,
- preserve freedom around what becomes open core and what becomes paid product,
- use real experiment deployments to discover the right monetization boundary.

## 11. Final Position

Orchestral should aim for a future where:

- the foundational experiment-operating-system layer is trusted and widely inspectable,
- the commercial value comes from packaging, validated integrations, premium workflows, deployment, and support,
- and the project grows from real lab adoption rather than from a purely theoretical software business model.
