# Research: Scroll Over Tray Icon to Change Volume

## 1. Receiving wheel input over a notification icon

- **Decision**: On the icon's `WM_MOUSEMOVE` callback (sent with `NOTIFYICON_VERSION_4`), register
  for raw mouse input (`RegisterRawInputDevices`, usage page 1 / usage 2, `RIDEV_INPUTSINK`,
  target = the tray window). Handle `WM_INPUT`: if the cursor (`GetCursorPos`) is outside the icon
  rectangle (`Shell_NotifyIconGetRect`), unregister (`RIDEV_REMOVE`) and ignore; otherwise read
  `RAWMOUSE.usButtonFlags & RI_MOUSE_WHEEL` and the signed `usButtonData` delta.
- **Rationale**: The shell forwards clicks and moves to the icon's callback but never wheel
  messages. Raw input is passive: it can't delay or swallow anyone's input, and it costs nothing
  when not registered. Because every mouse movement produces a `WM_INPUT`, the registration is
  dropped on the first movement after the pointer leaves the icon. EarTrumpet uses the same
  approach.
- **Alternatives considered**:
  - `WH_MOUSE_LL` hook: sees every mouse event system-wide and sits in the input path, so a slow
    response lags the whole desktop. Rejected under principle II.
  - Polling the cursor position on a timer: adds idle work. Rejected under principle II.
  - Registering raw input permanently: a message for every mouse movement all day. Rejected.

## 2. Wheel delta to steps

- **Decision**: Accumulate `usButtonData` (signed, 120 per notch). Emit `accumulated / 120` whole
  steps and keep the remainder. Reset the remainder when the registration is dropped.
- **Rationale**: Matches `WHEEL_DELTA` semantics, so high-resolution wheels and precision
  touchpads (smaller deltas) add up to one step per notch's worth (FR-007).
- **Alternatives considered**: One step per message regardless of delta. Rejected because smooth
  wheels would change volume many times per physical notch.

## 3. Step size and mute

- **Decision**: 2 points per notch, clamped to 0–100, computed from the rounded current percentage.
  Scroll up while muted calls `SetMutedAsync(Master, false)` before setting the level.
- **Rationale**: The Windows 11 volume icon uses 2 points; volume keys unmute on up.

## 4. Applying the change

- **Decision**: `App.ScrollVolume(steps)` adds to a pending step count. If no send is in flight, it
  loops: take the pending steps, `GetVolumesAsync()` for a fresh Master level, compute the target,
  set mute and volume, `GetVolumesAsync()` again, push the result to `TrayIcon.SetVolume` and
  `FlyoutWindow.ShowVolumes`. Errors drop the pending steps silently.
- **Rationale**: Reading fresh covers changes made in GG or by hotkeys just before scrolling.
  Coalescing is the same "latest value only" pattern the flyout's sliders already use. Re-reading
  after the set returns the other channels, which Sonar rescales with Master, so an open flyout
  shows correct values for every row.
- **Alternatives considered**: Keeping a cached level from the 1-second poll. Rejected because it
  can be a second stale.

## 5. Flyout while open

- **Decision**: Add `FlyoutWindow.ShowVolumes(volumes)` that updates the rows when the window is
  visible and no slider send is in flight for that row.
- **Rationale**: Today the flyout reads volumes only when it opens and after its own slider
  changes. It must not fight a slider the user is dragging.

## 6. Process-wide raw input registration

- **Finding**: Raw input registration is per process per device usage. The app registers nothing
  else for the mouse, and WinUI's pointer input does not depend on a raw mouse registration, and
  legacy mouse messages are still generated (no `RIDEV_NOLEGACY`). The quickstart checks the open
  flyout still scrolls and drags normally while the pointer hovers the icon.
