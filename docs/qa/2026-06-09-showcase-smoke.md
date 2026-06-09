# QA Report - 2026-06-09

## Build

- Commit under test: working tree based on `d3cdfe7`
- Unity version: `6000.4.10f1`
- Build target: Windows x86_64
- Build path: `Builds/Windows/CareerQuestCampus.exe`
- Build size: `95,174,318` bytes total, `667,648` byte executable
- Build log path: `Logs/ce-work-build.log`
- itch.io page: not created in this pass
- WebGL preview: not tested

## Environment

- Machine: local Windows workstation
- OS: Microsoft Windows 11 Pro `10.0.26200`
- Input devices: keyboard and mouse
- Same-computer host/client tested: yes
- LAN tested: no

## Smoke Matrix

| Check | Result | Notes |
|---|---|---|
| Unity opens cleanly | PASS | Batchmode bootstrap completed with exit code `0`. |
| Packages restore | PASS | Unity loaded Netcode, Transport, UGUI, and Test Framework packages. |
| EditMode tests | PASS | `11/11` passed in `Logs/ce-work-editmode-results.xml`. |
| PlayMode tests | PASS | `6/6` passed in `Logs/ce-work-playmode-results.xml`. |
| Showcase disclaimer appears before guided tour | PASS | Covered by PlayMode route tests and Showcase app flow. |
| Showcase completes under three minutes | PASS | Command smoke reaches Showcase reveal-ready state in seconds. |
| Play enters unseeded free-campus flow | PASS | Covered by PlayMode route tests and solo/host smoke modes. |
| Host P1 starts | PASS | Host smoke exits `0`; `connectedClients=2` after client joins. |
| Join Localhost as P2 | PASS | Client smoke exits `0`; connected line reports `connectedClients=2`. |
| Split controls in campus | PASS | Control presets are rendered and covered by PlayMode connection tests. |
| Split controls in Design Build Studio | NOT TESTED | Needs manual visual two-client input pass. |
| LAN manual IP join | NOT TESTED | UI/config path implemented; separate-machine LAN was not attempted. |
| Solo Fallback | PASS | Solo smoke exits `0` with `sessionMode=SoloFallback`. |
| Design Build Studio accepted placement | PASS | Rule and PlayMode flow tests cover accepted placement/result feedback. |
| Design Build Studio shared placement | NOT TESTED | Network state component is present; manual two-client placement was not clicked. |
| Health Hero Clinic completes | PASS | EditMode rules and PlayMode optional activity flow pass. |
| Logic Court completes | PASS | EditMode rules and PlayMode optional activity flow pass. |
| Achievement Gallery badge/source | PASS | Session/reveal tests and Showcase flow cover the path. |
| Career Reveal locked before three games | PASS | `Logs/avatar-solo-smoke.log` reports `revealReady=False`; scoring tests were updated for the 3-game gate. |
| Career Reveal unlock after three games | PASS | `Logs/avatar-showcase-smoke.log` reports `revealReady=True` after seeded three-game Showcase route. |
| Reveal confidence phrase | PASS | Career reveal scoring tests cover phrase behavior. |
| Debug overlay toggle | PASS | PlayMode debug overlay test passes. |
| No account/profile persistence/analytics/chat added | PASS | No persistence, account, telemetry, analytics, or chat code was added. |
| Forced host failure | NOT TESTED | Manual fault-injection pass still needed. |
| Forced join failure | NOT TESTED | Manual fault-injection pass still needed. |
| Disconnect recovery | NOT TESTED | Manual fault-injection pass still needed. |
| Mini-game timeout recovery | NOT TESTED | No timeout path was added in this pass. |
| Windows build runs | PASS | Host, client, Showcase, and Solo smoke all exit `0`. |
| WebGL preview loads | NOT TESTED | Windows build remains the multiplayer proof. |

## Evidence

- Bootstrap log: `Logs/ce-work-bootstrap.log`
- EditMode results: `Logs/ce-work-editmode-results.xml`
- PlayMode results: `Logs/ce-work-playmode-results.xml`
- Build log: `Logs/ce-work-build.log`
- Current visual/gameplay build log: `Logs/avatar-build.log`
- Current solo smoke: `Logs/avatar-solo-smoke.log`
- Current Showcase smoke: `Logs/avatar-showcase-smoke.log`
- Host smoke: `Logs/ce-work-host-smoke.log`
- Client smoke: `Logs/ce-work-client-smoke.log`
- Showcase smoke: `Logs/ce-work-showcase-smoke.log`
- Solo smoke: `Logs/ce-work-solo-smoke.log`
- Host/client proof: host and client both report live `connectedClients=2`.
- Showcase proof: `CQ_SMOKE_RESULT mode=showcase ... sessionMode=Showcase revealReady=True`.

## Known Issues

- LAN manual IP join is implemented but not tested on separate computers.
- Same-computer two-client smoke proves Netcode connection from the Windows build, but visual movement and Design Build shared placement still need a manual screen pass.
- WebGL preview and itch.io distribution were not part of this pass.
- Unity batchmode logs include licensing handshake chatter, but all relevant commands exited `0`.

## Demo Notes

- Use `Showcase` first for the polished under-three-minute evaluator route.
- Use `Play` for honest free-campus exploration and live host/client proof.
- Use Windows build as the multiplayer proof artifact.
