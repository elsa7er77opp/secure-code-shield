# Scan Task Orchestration

## Objective

Introduce an application-layer coordinator that owns scan task lifecycle and returns UI-safe progress snapshots. The coordinator must not read source files itself, hold raw evidence, or depend on Avalonia.

## Tasks

| Order | Task | Verification |
|---|---|---|
| 1 | Define a failing contract for task registration. | A unique task ID produces an initial pending snapshot. |
| 2 | Define a failing contract for progress transition. | Progress only advances while running and never exceeds eligible files. |
| 3 | Define a failing contract for cancellation. | A running task changes through cancelling to cancelled and is no longer cancellable. |
| 4 | Implement the minimum in-memory coordinator. | Application tests pass with no UI dependency. |
| 5 | Run full solution tests and document the public task-state boundary. | No regression in scanner or desktop tests. |

## Security and Performance Rules

The coordinator stores task IDs, counters, state, and safe failure categories only. It never stores paths, source content, secrets, or findings. All state mutations are serialized through an in-memory lock; a later persistence adapter may record safe task summaries without changing the public contract.
