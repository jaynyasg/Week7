# QA Report - 2026-06-09

## Build

- Commit: this bootstrap change
- Unity version: `6000.4.10f1`
- Build target: not built
- Build path: not applicable
- Build size: not applicable
- Build log path: not applicable
- itch.io page: not created
- WebGL preview: not created

## Environment

- Machine: local Windows workstation
- OS: Windows
- Input devices: not tested
- Same-computer host/client tested: no
- LAN tested: no

## Smoke Matrix

| Check | Result | Notes |
|---|---|---|
| Unity opens cleanly | PASS | Batchmode open wrapper reported `UNITY_EXIT=0`; Unity log ends with return code `0`. |
| Packages restore | PASS | `Packages/packages-lock.json` resolved Netcode for GameObjects `2.11.2` and Unity Transport `2.7.2`. |
| Ads/Analytics/Purchasing absent | PASS | These template extras are not direct dependencies in `Packages/manifest.json` or the generated lock. |
| Host P1 starts | NOT TESTED | Gameplay connection screen not implemented yet. |
| Join Localhost as P2 | NOT TESTED | Gameplay connection screen not implemented yet. |
| LAN manual IP join | NOT TESTED | LAN is planned but not yet implemented. |
| Solo Fallback | NOT TESTED | Solo fallback is planned but not yet implemented. |
| Windows build runs | NOT TESTED | No build created in this checkpoint. |
| WebGL preview loads | NOT TESTED | WebGL remains preview-only. |

## Evidence

Command wrapper:

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe"
$project = "C:\Users\jaynyasg\OneDrive\Documents\GitLab\Week7"
$log = "$env:TEMP\week7-unity-open-verify.log"
$args = @("-batchmode", "-nographics", "-quit", "-disable-assembly-updater", "-projectPath", $project, "-logFile", $log)
$process = Start-Process -FilePath $unity -ArgumentList $args -PassThru -WindowStyle Hidden
$process.WaitForExit(600000)
"UNITY_EXIT=$($process.ExitCode)"
```

Result:

```text
UNITY_EXIT=0
Batchmode quit successfully invoked - shutting down!
Exiting batchmode successfully now!
Exiting without the bug reporter. Application will terminate with return code 0
```

Log notes:

- The log includes a transient startup licensing handshake/access-token retry, followed by successful license initialization and entitlement resolution.
- The log includes `Curl error 42: Callback aborted` during shutdown, after `Batchmode quit successfully invoked`; Unity still terminated with return code `0`.

## Known Issues

- No gameplay scene exists yet.
- Same-computer two-client, LAN, and solo fallback flows remain implementation work.

## Demo Notes

- This checkpoint proves the repo is a clean Unity project with networking packages installed.
