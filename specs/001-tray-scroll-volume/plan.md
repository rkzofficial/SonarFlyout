# Implementation Plan: Scroll Over Tray Icon to Change Volume

**Branch**: `001-tray-scroll-volume` | **Date**: 2026-11-01 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-tray-scroll-volume/spec.md`

## Summary

Scrolling the mouse wheel over Sonar Flyout's tray icon changes Sonar's Master volume by 2 points
per notch, the way the Windows volume icon does. The shell never sends wheel messages to a
notification icon, so the tray window registers for raw mouse input only while the pointer is over
the icon (it learns that from the icon's `WM_MOUSEMOVE` callback) and unregisters as soon as the
pointer leaves the icon's rectangle. Wheel notches go to `App`, which reads the current Master
level fresh from Sonar, applies the steps (unmuting on scroll up), and pushes the new volumes to the
tray icon and, if open, the flyout. Requests are coalesced so fast scrolling never queues up.

## Technical Context

**Language/Version**: C# on .NET (`net10.0-windows10.0.26100.0`), Native AOT

**Primary Dependencies**: Windows App SDK / WinUI 3; Win32 via `LibraryImport` (user32, shell32)

**Storage**: N/A

**Testing**: Manual acceptance against the real app installed with `tools/install-dev.ps1`
(the project has no automated test suite); scenarios in [quickstart.md](quickstart.md)

**Target Platform**: Windows 11 (10.0.22000+), x64

**Project Type**: Desktop tray app (single project at repository root)

**Performance Goals**: New level visible in the tooltip within 0.5 s of the last notch; no
measurable CPU when the pointer is away from the icon

**Constraints**: No third-party code; AOT-safe; no new polling or timers; no system-wide hooks

**Scale/Scope**: About 80 changed lines across `TrayIcon.cs`, `Native.cs`, `App.xaml.cs`,
`FlyoutWindow.xaml.cs`, plus README and the 13 store listings

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
| --- | --- | --- |
| I. Native Microsoft Stack Only | Pass | Raw Input and `Shell_NotifyIconGetRect` are Win32, declared with `LibraryImport` |
| II. Fast and Light | Pass, with justification | Raw input is registered only while the pointer is over the icon, so idle cost is unchanged. While hovering, the tray window receives one message per mouse movement; that is the only added work and it ends when the pointer leaves. No timers, no hooks, no extra polling |
| III. Feels Like Windows | Pass | Matches the Windows volume icon: 2 points per notch, no window opens, icon and tooltip follow the level |
| IV. Localized Everywhere | Pass | No new UI strings. The listing feature bullet and README are updated in all 13 languages in the same change |
| V. Local-Only and Private | Pass | Uses only the existing local Sonar endpoints; failures are swallowed like the existing tray poll |
| VI. Minimal Code | Pass | No new classes; one callback on `TrayIcon`, one method on `App`, one on `FlyoutWindow` |
| VII. Release Discipline | Pass | No manifest or identity change; ships in a later version tag after 1.0.0 is live |

Post-design re-check: unchanged, all pass.

## Project Structure

### Documentation (this feature)

```text
specs/001-tray-scroll-volume/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── tray-wheel.md
└── tasks.md             # created by /speckit-tasks
```

### Source Code (repository root)

```text
Native.cs                # + RegisterRawInputDevices, GetRawInputData, Shell_NotifyIconGetRect, structs, constants
TrayIcon.cs              # + hover-scoped raw input, wheel accumulation, onWheel(steps) callback
App.xaml.cs              # + ScrollVolume(steps): fresh read, apply, coalesce, push to tray and flyout
FlyoutWindow.xaml.cs     # + ShowVolumes(volumes) so an open flyout follows external changes
README.md                # + "Scroll over the tray icon" row in the controls table
store/listings/*.md      # + feature bullet in all 13 languages
```

**Structure Decision**: Single project at the repository root, matching the existing layout. No
new files in the app itself.

## Complexity Tracking

No violations to justify.
