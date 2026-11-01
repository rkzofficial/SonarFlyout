---

description: "Task list for Scroll Over Tray Icon to Change Volume"
---

# Tasks: Scroll Over Tray Icon to Change Volume

**Input**: Design documents from `specs/001-tray-scroll-volume/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/tray-wheel.md](contracts/tray-wheel.md)

**Tests**: No automated tests (not requested; the project has no test suite). Each story ends with
manual checks from [quickstart.md](quickstart.md) against the app installed by
`tools/install-dev.ps1`.

**Organization**: Tasks are grouped by user story. All source files are at the repository root.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

---

## Phase 1: Setup

No setup: the project, toolchain and packaging already exist.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Win32 declarations the tray icon needs to receive wheel input.

- [X] T001 Add to `Native.cs` (as `LibraryImport`, matching the file's style): constants
  `WM_MOUSEMOVE = 0x0200`, `WM_INPUT = 0x00FF`, `RIDEV_REMOVE = 0x1`, `RIDEV_INPUTSINK = 0x100`,
  `RID_INPUT = 0x10000003`, `RIM_TYPEMOUSE = 0`, `RI_MOUSE_WHEEL = 0x0400`; structs
  `RAWINPUTDEVICE` (ushort usUsagePage, ushort usUsage, uint dwFlags, nint hwndTarget),
  `RAWINPUTHEADER` (uint dwType, uint dwSize, nint hDevice, nint wParam), `RAWMOUSE` (ushort
  usFlags, ushort usButtonFlags, ushort usButtonData at the union offset, uint ulRawButtons, int
  lLastX, int lLastY, uint ulExtraInformation; `[StructLayout(Sequential)]` with the
  `usButtonFlags`/`usButtonData` pair placed after a 2-byte pad so `usButtonFlags` sits at offset 4),
  `RAWINPUT` (header + RAWMOUSE), `NOTIFYICONIDENTIFIER` (uint cbSize, nint hWnd, uint uID, Guid
  guidItem); functions `RegisterRawInputDevices(RAWINPUTDEVICE*, uint, uint)` (user32),
  `GetRawInputData(nint, uint, void*, ref uint, uint)` (user32),
  `Shell_NotifyIconGetRect(NOTIFYICONIDENTIFIER*, RECT*)` returning HRESULT (shell32)

**Checkpoint**: Project builds in Release with the new declarations unused.

---

## Phase 3: User Story 1 - Adjust volume by scrolling over the tray icon (Priority: P1) 🎯 MVP

**Goal**: Each wheel notch over the tray icon changes Master by 2 points, without opening anything.

**Independent Test**: quickstart scenarios 1, 2, 3, 7, 8, 9, 10, 11.

- [X] T002 [US1] In `TrayIcon.cs`, add an `Action<int> onWheel` constructor parameter (signed
  notch count, positive = up) stored in a field, plus fields `bool _wheelRegistered` and
  `int _wheelRemainder`
- [X] T003 [US1] In `TrayIcon.cs` `WndProc`, on the icon callback's `WM_MOUSEMOVE` (low word of
  `lParam`), if not registered, call `RegisterRawInputDevices` for usage page 1 / usage 2 with
  `RIDEV_INPUTSINK` and `hwndTarget = _hwnd`, and set `_wheelRegistered`
- [X] T004 [US1] In `TrayIcon.cs` `WndProc`, handle `WM_INPUT`: read the input with
  `GetRawInputData(lParam, RID_INPUT, ...)` into a stack `RAWINPUT`; get the icon rectangle with
  `Shell_NotifyIconGetRect` (hWnd `_hwnd`, uID 1) and the cursor with `GetCursorPos`; if the cursor
  is outside the rectangle or the call fails, unregister (`RIDEV_REMOVE`, `hwndTarget = 0`), clear
  `_wheelRegistered`, set `_wheelRemainder = 0`; otherwise, for a mouse input with
  `RI_MOUSE_WHEEL`, add `(short)usButtonData` to `_wheelRemainder`, and if
  `_wheelRemainder / 120 != 0` call `_onWheel` with that quotient and keep `_wheelRemainder % 120`.
  Ignore `RI_MOUSE_HWHEEL`. Pass `WM_INPUT` on to `DefWindowProcW`
- [X] T005 [US1] In `TrayIcon.Dispose` (`TrayIcon.cs`), unregister raw input if `_wheelRegistered`
- [X] T006 [US1] In `App.xaml.cs`, add fields `int _pendingSteps` and `bool _sendingSteps`, and an
  `async void ScrollVolume(int steps)` that adds to `_pendingSteps` and returns if a send is in
  flight; otherwise loops while `_pendingSteps != 0`: take and clear the steps, read
  `GetVolumesAsync()` for the current Master, compute the target as
  "clamp(round(current × 100) + 2 × steps, 0, 100) / 100", call `SetVolumeAsync(Master, target)`
  only if it differs, read `GetVolumesAsync()` again and pass Master to `_tray.SetVolume`. Catch
  every exception, clear `_pendingSteps`, and show nothing (same as the existing poll). Clear
  `_sendingSteps` in `finally`. In the existing poll tick, skip `_tray.SetVolume` while
  `_sendingSteps` is true so a poll that started before the scroll can't put the old level back on
  the icon
- [X] T007 [US1] In `App.OnLaunched` (`App.xaml.cs`), pass `ScrollVolume` to the `TrayIcon`
  constructor
- [X] T008 [US1] Build Release MSIX, install with `tools/install-dev.ps1`, run quickstart scenarios
  1, 2, 3, 7, 8, 9, 10, 11

**Checkpoint**: Scrolling over the icon changes volume; the tray icon and tooltip already follow
via `_tray.SetVolume`.

---

## Phase 4: User Story 2 - See the new level right away (Priority: P2)

**Goal**: An open flyout follows the change, and the tooltip and icon update promptly.

**Independent Test**: quickstart scenarios 4 and 6.

- [X] T009 [US2] In `FlyoutWindow.xaml.cs`, add `public void ShowVolumes(IReadOnlyDictionary<string,
  ChannelVolume> volumes)` that returns if the window is not visible, and otherwise updates each row
  in `_rows` whose channel is in `volumes` and not in `_sendingVolumes`, the same way
  `RefreshOtherVolumesAsync` does (use the dictionary type `GetVolumesAsync` returns)
- [X] T010 [US2] In `App.ScrollVolume` (`App.xaml.cs`), after the second `GetVolumesAsync()`, also
  call `_flyout.ShowVolumes(volumes)`
- [X] T011 [US2] Build, install, run quickstart scenarios 4 and 6 (including dragging and
  scrolling sliders inside the open flyout)

**Checkpoint**: Flyout sliders track tray scrolling and still work normally.

---

## Phase 5: User Story 3 - Scrolling while muted (Priority: P3)

**Goal**: Scroll up unmutes Master; scroll down keeps it muted.

**Independent Test**: quickstart scenario 5.

- [X] T012 [US3] In `App.ScrollVolume` (`App.xaml.cs`), when steps are positive and Master is
  muted, call `SetMutedAsync(Master, false)` before setting the volume, even when the level can't
  change (muted at 100%)
- [X] T013 [US3] Build, install, run quickstart scenario 5

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T014 [P] In `README.md`, add a row "Scroll over the tray icon | Change master volume" to the
  controls table, after the "Click the tray icon" row
- [X] T015 [P] In each of the 13 files in `store/listings/` (en-US, de-DE, fr-FR, es-ES, it-IT,
  pt-BR, ru-RU, pl-PL, tr-TR, ja-JP, ko-KR, zh-Hans, zh-Hant), add a "Product features" bullet
  after the tray-icon bullet: "Scroll over the tray icon to change master volume", translated
- [X] T016 Run quickstart scenario 12 (light and dark taskbar, 150% scaling) and confirm the
  Release MSIX build has no new warnings

---

## Dependencies & Execution Order

- **Foundational (T001)** blocks US1.
- **US1 (T002–T008)** blocks US2 and US3, which both extend `App.ScrollVolume`.
- **US2 (T009–T011)** and **US3 (T012–T013)** touch different parts of `App.ScrollVolume`; do
  them one after the other to avoid conflicting edits in `App.xaml.cs`. T009 (`FlyoutWindow.xaml.cs`)
  can run alongside US3.
- **Polish**: T014 and T015 can start any time; T016 last.

### Parallel Opportunities

- T014 and T015 (docs and listings) in parallel with any code task.
- T009 in parallel with T012.
- Within US1, tasks are sequential (T002–T005 share `TrayIcon.cs`, T006–T007 share
  `App.xaml.cs`), though the `TrayIcon.cs` and `App.xaml.cs` groups can be written in parallel.

## Implementation Strategy

1. T001, then US1 → validate → this alone delivers the feature the user asked for.
2. US2 → validate the open flyout.
3. US3 → validate mute.
4. Polish, full quickstart pass, then commit on `001-tray-scroll-volume`. Release as a new
   version tag only after 1.0.0 is live in the Store.
