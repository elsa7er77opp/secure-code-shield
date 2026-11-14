# Project Identity and Local Path Scope

## Objective

Add a local project identity model and a narrow path-scope contract before introducing a persisted project workflow. The feature must preserve the product boundary: a scan may read only the user-authorized canonical root and must not infer authorization from a matching path string.

## Tasks

| Order | Task | Verification |
|---|---|---|
| 1 | Add a failing domain contract for stable project identity and a normalized display name. | Unit test rejects blank IDs and names; accepted identity is immutable. |
| 2 | Add a failing contract for canonical-root scope. | Unit test accepts child paths, rejects sibling-prefix paths and paths outside the root. |
| 3 | Implement the smallest domain model and policy that satisfy the contracts. | Domain tests pass without filesystem or Avalonia dependencies. |
| 4 | Add an application request for registered local projects that requires explicit authorization. | Application test proves no project registration without a valid authorization decision. |
| 5 | Record the new contract and update the build TODO. | Full solution tests pass. |

## Security Invariants

The system stores a canonical local root path only after explicit user authorization. It treats a path as in-scope only when it equals the root or begins with a directory separator after the canonical root. It never treats a simple string prefix as containment, follows no reparse point outside the root, and keeps the root path out of shared reports.

## Rollback

All work is isolated in new domain/application files and tests. If a contract or implementation is incorrect, the change can be reverted without affecting the existing scanner, account service, or desktop UI.
