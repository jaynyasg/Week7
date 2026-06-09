# QA Evidence

Create one dated report per serious test/build pass.

Suggested filename:

```text
docs/qa/YYYY-MM-DD-build-smoke.md
```

## Report Template

```markdown
# QA Report - YYYY-MM-DD

## Build

- Commit:
- Unity version:
- Build target:
- Build path:
- Build size:
- Build log path:
- itch.io page:
- WebGL preview:

## Environment

- Machine:
- OS:
- Input devices:
- Same-computer host/client tested: yes/no
- LAN tested: yes/no

## Smoke Matrix

| Check | Result | Notes |
|---|---|---|
| Unity opens cleanly | PASS/FAIL | |
| Packages restore | PASS/FAIL | |
| Host P1 starts | PASS/FAIL | |
| Join Localhost as P2 | PASS/FAIL | |
| Split controls in campus | PASS/FAIL | |
| Split controls in Design Build Studio | PASS/FAIL | |
| LAN manual IP join | PASS/FAIL/NOT TESTED | |
| Solo Fallback | PASS/FAIL | |
| Design Build Studio shared placement | PASS/FAIL | |
| Health Hero Clinic completes | PASS/FAIL | |
| Logic Court completes | PASS/FAIL | |
| Passport stamp tier/source | PASS/FAIL | |
| Career Reveal unlock after two games | PASS/FAIL | |
| Reveal confidence phrase | PASS/FAIL | |
| Debug overlay toggle | PASS/FAIL | |
| Forced host failure | PASS/FAIL | |
| Forced join failure | PASS/FAIL | |
| Disconnect recovery | PASS/FAIL | |
| Mini-game timeout recovery | PASS/FAIL | |
| Windows build runs | PASS/FAIL | |
| WebGL preview loads | PASS/FAIL/NOT TESTED | |

## Evidence

- Screenshot 1:
- Screenshot 2:
- Short video:

## Known Issues

- 

## Demo Notes

- 
```

## QA Rules

- Same-computer host/client testing is required.
- LAN support is optional unless tested and documented.
- WebGL is preview-only unless networking is already working.
- Solo Fallback is allowed, but must be labeled as fallback/demo mode.
- Career Reveal stays celebratory; source metadata belongs in Passport/debug.
- Failed or partial results should produce `Practice` stamps, not harsh failure copy.
