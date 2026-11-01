# Feature Specification: Scroll Over Tray Icon to Change Volume

**Feature Branch**: `001-tray-scroll-volume`

**Created**: 2026-11-01

**Status**: Draft

**Input**: User description: "Scroll the mouse wheel over the tray icon to change volume, like the Windows volume icon. While the pointer hovers over Sonar Flyout's tray icon, scrolling up raises and scrolling down lowers the volume, without opening the flyout. The app replaces the Windows volume icon, so this should match its behavior: same step size per notch, tray icon/tooltip updates to the new level, works when the flyout is closed or open, and does nothing harmful when Sonar is unreachable."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Adjust volume by scrolling over the tray icon (Priority: P1)

A user who has replaced the Windows volume icon with Sonar Flyout rests the pointer on Sonar
Flyout's tray icon and turns the mouse wheel. Each notch up raises Sonar's Master volume by one
step; each notch down lowers it by one step. Nothing opens or takes focus, so the user keeps
working in whatever app they were using.

**Why this priority**: This is the whole feature and the gap the user hit in daily use. The
Windows volume icon supports it, so its absence makes Sonar Flyout a worse replacement.

**Independent Test**: With the flyout closed, hover over the tray icon, scroll one notch up and one
notch down, and check that Sonar's Master volume (as shown in SteelSeries GG) rises and falls by
one step each time, with no window opening.

**Acceptance Scenarios**:

1. **Given** Master volume is 50% and the flyout is closed, **When** the user scrolls one notch up
   over the tray icon, **Then** Master volume becomes 52% and no window opens or takes focus.
2. **Given** Master volume is 50%, **When** the user scrolls one notch down over the tray icon,
   **Then** Master volume becomes 48%.
3. **Given** the user scrolls several notches quickly, **When** they stop, **Then** Master volume
   has changed by one step per notch and ends at the expected level.
4. **Given** Master volume is 99%, **When** the user scrolls up, **Then** it stops at 100%; given
   1%, scrolling down stops at 0%.
5. **Given** the pointer is anywhere other than Sonar Flyout's tray icon, **When** the user
   scrolls, **Then** Master volume does not change and the scroll reaches the app under the
   pointer as usual.

---

### User Story 2 - See the new level right away (Priority: P2)

While scrolling, the user sees the result without opening anything: the tray icon's glyph and
its tooltip reflect the new level, and if the flyout is open its Master slider moves too.

**Why this priority**: Without feedback the user cannot tell where the volume landed. The
Windows icon shows the level in its tooltip while scrolling.

**Independent Test**: Hover over the tray icon until the tooltip shows, scroll, and check that the
tooltip percentage and the icon glyph update to match Sonar's Master volume.

**Acceptance Scenarios**:

1. **Given** the tooltip is showing "Volume: 50%", **When** the user scrolls one notch up,
   **Then** the tooltip shows the new level within 0.5 seconds.
2. **Given** volume crosses a glyph threshold (for example from 66% to 68%), **When** the user
   scrolls, **Then** the tray icon switches to the matching glyph.
3. **Given** the flyout is open, **When** the user scrolls over the tray icon, **Then** the
   flyout's Master slider shows the new level and the flyout stays open.

---

### User Story 3 - Scrolling while muted (Priority: P3)

A user whose Master is muted scrolls up over the tray icon to bring sound back, the same way the
Windows volume keys unmute.

**Why this priority**: A common moment, but less frequent than plain adjustment.

**Independent Test**: Mute Master, scroll one notch up over the tray icon, and check Master is
unmuted and one step higher.

**Acceptance Scenarios**:

1. **Given** Master is muted at 40%, **When** the user scrolls one notch up, **Then** Master is
   unmuted and at 42%.
2. **Given** Master is muted at 40%, **When** the user scrolls one notch down, **Then** Master
   stays muted and moves to 38%.

### Edge Cases

- **Sonar unreachable**: scrolling over the icon does nothing visible, does not crash or freeze
  the app, and does not show an error popup; the existing tray behavior stays as it is, and
  scrolling works again once Sonar is back.
- **High-resolution wheels and touchpads**: input that arrives in fractions of a notch adds up,
  and one step is applied for every full notch's worth of scrolling.
- **Very fast scrolling**: the app never falls behind Sonar by more than the latest requested
  level, and the final level matches the number of notches scrolled.
- **Tray icon in the overflow area**: scrolling over the icon while it sits in the hidden-icons
  flyout behaves the same as on the taskbar.
- **Volume changed elsewhere** (in GG or another app) just before scrolling: the step is applied
  to the current level, not a stale one.
- **Multiple monitors and display scaling**: hovering is detected correctly on any monitor that
  shows the taskbar, at any scaling.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While the pointer is over Sonar Flyout's tray icon, each notch of mouse wheel
  movement up MUST raise Sonar's Master volume by 2 percentage points, and each notch down MUST
  lower it by 2 points.
- **FR-002**: Volume MUST stay within 0% and 100%.
- **FR-003**: Scrolling MUST NOT open, close, or focus any window, and MUST NOT take the wheel
  input away from other apps when the pointer is not over the tray icon.
- **FR-004**: After each change, the tray icon glyph and tooltip MUST reflect the new level.
- **FR-005**: If the flyout is open, its Master control MUST show the new level, and the flyout
  MUST stay open.
- **FR-006**: Scrolling up while Master is muted MUST unmute it; scrolling down MUST NOT unmute.
- **FR-007**: Partial-notch input MUST accumulate so that one full notch equals one step.
- **FR-008**: When Sonar cannot be reached, scrolling MUST fail silently, MUST NOT crash, hang or
  slow the app, and MUST work again once Sonar is reachable.
- **FR-009**: When the pointer is not over the tray icon, the feature MUST add no ongoing work to
  the app beyond what already runs while the flyout is closed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can raise or lower volume from the tray icon in under 1 second without
  clicking anything.
- **SC-002**: Scrolling 10 notches up from 50% leaves Master at exactly 70%, every time.
- **SC-003**: The tooltip and icon show the new level within 0.5 seconds of the last notch.
- **SC-004**: With Sonar not running, 50 notches of scrolling cause no crash, freeze, or visible
  error.
- **SC-005**: With the pointer away from the tray icon, the app's idle CPU use is unchanged from
  the current release.

## Assumptions

- "Volume" means Sonar's Master channel, the same level the tray icon already displays.
- The step size of 2 points per notch matches the Windows 11 volume icon.
- Scrolling over the icon does not open the tooltip by itself; the tooltip updates if Windows is
  already showing it.
- No setting to turn this off or change the step size is needed.
- Only vertical scrolling changes the volume; horizontal scrolling is ignored.
