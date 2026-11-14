# Secure Code Shield — Windows Alpha 1.0.0

## Package

The Windows x64 Alpha distribution is located at:

```text
artifacts/CyberBank-Secure-Code-Shield-Alpha-win-x64.zip
```

It contains the self-contained Release application and its required native runtime libraries. Extract the archive to a local folder and start `SecureCodeShield.Desktop.exe` from that folder.

| Item | Value |
|---|---|
| Target | Windows x64 |
| Deployment mode | Self-contained Release bundle |
| Archive size | 50,653,389 bytes |
| Archive SHA-256 | `51B2464F7A2407575D1C65882AA12B6FAA9634840EA1EC45C17278D11C6AE210` |
| Verified launch | Opens `Secure Code Shield — Local Sign In` from the published executable |

## Validated behavior

The release was produced after the full solution test suite passed. The test suite covers local authorization, safe evidence masking, allowed-file policies, cancellation states, safe report export, local account protection, task coordination, SQLite corruption recovery, migration backup, scan-summary persistence, and audit-event persistence.

The first-run sequence is splash screen, local credential setup or sign-in, and then the Analyst Workstation. The app does not upload source code, execute selected project files, save raw passwords, or store raw secret evidence.

## Distribution boundary

This is an **Alpha ZIP distribution**, not yet a signed MSI/MSIX/EXE installer. Windows SmartScreen and other endpoint tools may warn about unsigned Alpha software. A signed installer, updater channel, and clean-machine installation record remain release-readiness work and should precede a public commercial release.
