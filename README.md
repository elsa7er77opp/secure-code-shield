# Secure Code Shield

A local static review workstation checks a user-authorized project folder, masks evidence and exports sanitized reports. Domain rules, application coordination, infrastructure scanning and an Avalonia UI separate detection from report presentation.

## Engineering scope

The release record covers the installed local workflow. Its commercial verification decision remains conditional on clean-machine evidence and signing. Test fixtures deliberately contain credential-shaped data used to exercise detection and redaction.

## Getting started

Use the .NET 10 SDK on Windows. From the repository root, build with `dotnet build SecureCodeShield.slnx`. To run the isolated domain suite, use `dotnet test tests/SecureCodeShield.Domain.Tests/SecureCodeShield.Domain.Tests.csproj`. Start the desktop application with `dotnet run --project src/SecureCodeShield.Desktop/SecureCodeShield.Desktop.csproj`.

## Status and verification

Final 1.0.0 documented. Commercial verification conditional fail.

95 domain tests passed locally. Installer signing and clean-VM acceptance were not repeated.

## Source and evidence

- [src](src)
- [tests](tests)
- [Windows-Final-1.0.0.md](docs/release/Windows-Final-1.0.0.md)
- [verification-report.md](docs/release/final-commercial-verification/verification-report.md)

## Design record

[Production-Architecture.md](docs/architecture/Production-Architecture.md)

## Build on Windows

The local build environment uses the installed .NET SDK. In the current remote build session, the Windows environment requires the standard `ProgramFiles` and architecture variables to be present before invoking .NET commands. This is a machine-session workaround, not a product runtime requirement.

```text
dotnet test tests\SecureCodeShield.Domain.Tests\SecureCodeShield.Domain.Tests.csproj
```

## Scope control

Telegram, Discord, payment, remote scanning, cloud synchronization, and Web3 Collection Studio are outside this repository and must not be added without an explicit product decision.

## Author

Ahmed Mohamed Elsayed Elreefy. Engineering work includes requirements, technical decisions, AI-assisted implementation, debugging and acceptance review.
