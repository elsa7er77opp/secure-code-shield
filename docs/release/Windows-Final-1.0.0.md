# CyberBank Secure Code Shield — Windows Final 1.0.0

**Release date:** 2026-08-24  
**Distribution:** Native Windows Desktop, self-contained `win-x64`  
**Installation:** Current-user / lowest-privilege LocalAppData installation

## Final Scope

Secure Code Shield Final 1.0.0 is a local defensive code-review workstation for a folder explicitly authorized by the operator. The visible Workstation is now driven by actual local review state: project candidate, explicit authorization, local review readiness, cancellation state, redacted finding count, local audit status, and sanitized-report readiness. The shell does not present synthetic threat metrics, target controls, network activity, execution controls, or raw evidence.

| Workflow step | Final behavior |
|---|---|
| Choose project | Selects an accessible local folder only; selection alone does not authorize review. |
| Authorize | Explicitly authorizes the selected local root before review becomes available. |
| Start local review | Reads eligible local text through the existing defensive scanner; selected files are not executed. |
| Cancel | Stops the local review state and keeps report export unavailable until a completed review. |
| Findings | Exposes only a redacted count to the Desktop state; raw synthetic indicator values remain unavailable in report output. |
| Export report | Writes a sanitized local report after successful completion only. |

## Safety Boundary

> Secure Code Shield Final 1.0.0 is not a target scanner, network probe, exploit framework, shell launcher, secret vault, or cloud service. It performs bounded local review of an explicitly selected and authorized folder; no source, raw secret, or report is uploaded.

## Verification Evidence

| Verification | Result |
|---|---|
| Full regression (post-fix) | **36 passed / 0 failed**: Domain 11, Application 4, Infrastructure 12, Desktop 9. |
| New Final acceptance | Local authorized review of harmless fixture text produced one redacted finding and a sanitized report without the synthetic indicator value. |
| Release Desktop build | Succeeded with **0 warnings** and **0 errors**. |
| Self-contained publish | Completed for `win-x64`. |
| Installer compiler | Inno Setup 6.7.3 completed successfully. |
| Silent installation | Completed to `%LocalAppData%\Programs\CyberBank\Secure Code Shield`. |
| Installed end-to-end smoke (post-fix) | Installed binary completed local login → explicit authorization → one redacted local finding → sanitized report export → cancelled review → close/restart login smoke. |
| Optional Desktop Shortcut | **Passed.** Selecting the installer task `Create a desktop shortcut` (`desktopicon`) created `CyberBank Secure Code Shield.lnk` on Desktop, targeting the installed EXE and its icon. |

## Final Installer

| Property | Value |
|---|---|
| File | `artifacts\installer\CyberBank-Secure-Code-Shield-Final-Setup-win-x64.exe` |
| Version | `1.0.0 Final` |
| SHA-256 (post-fix artifact) | `404ED3A21A00A855E79E48240CA580A0B59BA8C9436384A14E3BAEE06C941882` |
| Install scope | Current Windows user; lowest privilege; LocalAppData. |
| Code signing | **Not code-signed.** Windows reputation warnings remain possible until a distribution certificate is supplied and a signed build is produced. |

## Visual Smoke

The installer-launched native Login window was captured with the approved local identity block, local-only privacy statement, username/password fields, and no remote account claim. The screenshot is retained at `docs/release/secure-code-shield-final-installed-login.png`.

## Commercial Verification Addendum

The original Final artifact was superseded by a post-fix 1.0.0 installer after a login-to-workstation lifecycle blocker was found during installed-binary verification. The post-fix artifact passes regression and the local-only installed workflow. However, the **commercial release decision is CONDITIONAL FAIL**: no independent clean Windows VM evidence exists, and `Get-AuthenticodeSignature` reports `NotSigned` for the installed executable. Do not issue a new commercial release stamp or claim clean-machine certification until both gates are completed. See `docs/release/final-commercial-verification/verification-report.md`.
