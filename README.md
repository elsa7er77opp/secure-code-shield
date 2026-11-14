# Secure Code Shield

Secure Code Shield is a **local-first Windows desktop workstation** for reviewing a project folder that its user explicitly authorizes. The application is being rebuilt from a clean codebase; it does not reuse the retired Electron prototypes.

The first version is intentionally defensive and bounded. It will analyze supported local text files without executing them, redact sensitive evidence before persistence or display, track scan progress and cancellation safely, and export sanitized reports. It will not upload source code, save raw secrets, or start any scan without explicit authorization.

## Architecture

The approved design is in [`docs/architecture/Production-Architecture.md`](docs/architecture/Production-Architecture.md). The desktop shell will use native-rendered Avalonia controls; analysis rules and local safety policies remain in framework-independent .NET projects.

## Current foundation

The clean solution currently establishes three tested domain contracts: explicit project authorization, safe evidence masking, and a visible scan-task cancellation protocol. The implementation will grow through test-first increments; generated fixtures must never contain real credentials or customer source.

## Build on Windows

The local build environment uses the installed .NET SDK. In the current remote build session, the Windows environment requires the standard `ProgramFiles` and architecture variables to be present before invoking .NET commands. This is a machine-session workaround, not a product runtime requirement.

```text
dotnet test tests\SecureCodeShield.Domain.Tests\SecureCodeShield.Domain.Tests.csproj
```

## Scope control

Telegram, Discord, payment, remote scanning, cloud synchronization, and Web3 Collection Studio are outside this repository and must not be added without an explicit product decision.
