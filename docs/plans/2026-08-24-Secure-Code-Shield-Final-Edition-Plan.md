# Secure Code Shield — Final Edition Implementation Plan

## Objective

Promote Secure Code Shield from a verified Alpha distribution to a **Final Edition candidate** without expanding its defensive boundary. The program remains a native Windows, local-first workstation for an explicitly authorized project folder. It may read eligible local text files without executing them, generate redacted findings, preserve safe metadata, and write a sanitized local report. It must not upload code, invoke a shell, execute selected files, store raw secrets, probe remote systems, or operate outside the chosen local root.

## Current Gap

The existing solution has meaningful tested Domain/Application/Infrastructure capabilities and a real local login gate. The production blocker is not a missing visual mock-up: the existing `MainViewModel` exposes only a project label and authorization text, while `MainWindow` wires only a folder picker. The visible Analyst Workstation is therefore mostly static and cannot demonstrate the safe local review, progress, findings, audit, and report flow that the Alpha release document claims.

## Final Edition Scope

| Included | Excluded |
|---|---|
| Explicit local project selection and authorization confirmation. | URLs, IPs, hosts, network scans, remote commands, shell controls. |
| Safe eligible-file inventory and redaction-first synthetic indicators through existing local contracts. | Code execution, process launches, package install, dependency resolution from the internet. |
| Visible review progress, cancellation, redacted finding list, audit summary, and sanitized local report state. | Raw secret display, raw source persistence, uploads, telemetry, cloud storage. |
| Reopenable local safe summaries and current-user installer verification. | Claims of malware protection, exploit validation, or code security certification. |

## Sequential Work Plan

### 1. Establish Final Desktop Contracts by TDD

Create focused Desktop/Application tests before changing production code. The desired contracts are: selecting a folder does **not** make it authorized; an explicit authorization action is required; a review cannot begin while blocked; a safe review state exposes progress/cancellation and redacted result counts; and report availability follows a completed safe review. Capture RED logs, then add the smallest adapters to existing Domain/Application capabilities to turn tests GREEN.

### 2. Build a Local Review Session Adapter

Create a dedicated adapter between `MainViewModel` and the existing safe review/orchestration contracts. It owns only view-safe projections: selected root display name, authorization state, eligible/excluded counts, task state, progress, redacted finding rows, current audit messages, and report readiness. It must not leak raw paths, source lines, secret values, or file bytes into the view model.

### 3. Bind the Existing Workstation to Real State

Preserve the approved dark GitHub-style visual language, but replace static `—`, `Awaiting a scan`, placeholder rows, and inactive commands with bindings driven by the session adapter. `Choose project`, `Authorize`, `Start local review`, `Cancel`, and `Export sanitized report` must have precise disabled/blocked states and local safety copy. Existing navigation remains context-only until its represented workflow is available; no fake pages or deceptive buttons are introduced.

### 4. Verify Normal and Safe-Failure Paths

Add an end-to-end local acceptance fixture containing harmless synthetic text and an indicator that never contains a real credential. Verify the sequence: first-run login → choose local fixture → explicit authorization → local review → cancel or complete → redacted finding summary → sanitized report. Verify negative paths: no project, no authorization, cancelled review, unsupported file, and report before completion.

### 5. Produce Final Windows Candidate Evidence

Run the complete test suite and record the precise total. Create a self-contained `win-x64` build, revise the Inno Setup metadata from Alpha to the selected Final version, install to a clean current-user location, launch the installed program, verify the login and local review smoke state, capture a native window, and record the installer SHA-256. Code signing remains a distribution decision; the release note must state clearly whether it is signed or unsigned.

## Acceptance Gate

Secure Code Shield is eligible for Final Edition only when its visible workstation is driven by the safe local workflow rather than static shell text, every new behavior has passed RED→GREEN tests, current Windows install/launch evidence exists, and the release note accurately describes both capabilities and boundaries.
