# Data Model: Scroll Over Tray Icon to Change Volume

No stored data. Runtime state only:

| State | Owner | Meaning | Transitions |
| --- | --- | --- | --- |
| Raw input registered | `TrayIcon` | Whether wheel input is currently being received | Off → On on the icon's `WM_MOUSEMOVE`; On → Off on the first `WM_INPUT` with the cursor outside the icon rectangle, and on dispose |
| Wheel remainder | `TrayIcon` | Signed wheel delta not yet worth a full notch (−119…119) | Reset to 0 when registration turns off |
| Pending steps | `App` | Signed notches not yet sent to Sonar | Grows with each notch; taken in full by the send loop; cleared on error |
| Sending | `App` | Whether a send loop is running | Set when the loop starts, cleared when pending steps reach 0 or on error |

Validation: target volume = clamp(round(current × 100) + 2 × steps, 0, 100) / 100.
