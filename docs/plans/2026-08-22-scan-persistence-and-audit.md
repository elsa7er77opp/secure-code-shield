# Scan Persistence and Safe Audit Records

## Objective

Persist only safe scan summaries and local audit events in the existing SQLite state store. The storage layer must never accept source text, raw secret matches, or report bodies.

## Tasks

| Order | Task | Verification |
|---|---|---|
| 1 | Add a failing integration contract for schema v2. | Initialization creates `scan_runs` and `audit_events` tables after `schema_migrations`. |
| 2 | Add a failing contract for a persisted safe scan summary. | Stored row includes IDs, timestamps, outcome, and counters only. |
| 3 | Add a failing contract for local audit records. | Audit record stores action/category/time with no source/evidence field. |
| 4 | Implement migration v2 and transactional repository methods. | Individual contracts pass. |
| 5 | Add pre-migration backup before future schema upgrades. | Integration test verifies backup path creation for an existing state store. |
| 6 | Run the full solution test suite. | All domain, application, infrastructure, and desktop tests stay green. |

## Data Boundary

`scan_runs` can contain scan ID, project ID, engine version, start/end UTC timestamps, outcome, eligible/processed/finding counters, and safe error category. `audit_events` can contain event ID, UTC timestamp, category, action, and task ID. Neither table may contain absolute paths, relative paths, source content, raw evidence, credentials, or report text.
