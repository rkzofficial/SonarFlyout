# Quickstart: validate tray scroll volume

## Setup

```powershell
powershell -File tools\install-dev.ps1
```

SteelSeries GG running with Sonar in Classic mode. Keep GG's Sonar page visible to cross-check
Master.

## Scenarios

1. **Up and down**: Set Master to 50% in GG. Hover the tray icon, scroll 1 notch up → 52%; 1 notch
   down → 50%. No window opens; the app in front keeps focus.
2. **Ten notches**: From 50%, scroll 10 notches up quickly → exactly 70%. Repeat three times.
3. **Bounds**: From 98%, scroll 5 up → 100%. From 2%, scroll 5 down → 0%.
4. **Feedback**: Hover until the tooltip shows, scroll; the tooltip percentage and icon glyph
   follow within half a second. Cross 33% and 67% to see the glyph change.
5. **Muted**: Mute Master at 40%. Scroll 1 down → muted, 38%. Scroll 1 up → unmuted, 40%.
6. **Flyout open**: Open the flyout, scroll over the tray icon; the Master slider and output
   channel sliders follow and the flyout stays open. Then drag sliders and scroll inside the
   flyout to confirm they still work normally.
7. **Elsewhere**: Scroll over a browser page and over another tray icon; Master doesn't change and
   the page scrolls.
8. **Overflow**: Move the icon into the hidden-icons flyout, open it, hover the icon, scroll; same
   as scenario 1.
9. **Touchpad / smooth wheel**: Two-finger scroll over the icon; volume changes in 2-point steps,
   not in bursts.
10. **Sonar unreachable**: Quit GG, scroll 50 notches over the icon; no crash, freeze or popup.
    Start GG again; scrolling works without restarting the app.
11. **Idle**: With the pointer away from the icon, Task Manager shows 0% CPU for Sonar Flyout, as
    before the change.
12. **Themes and scaling**: Repeat scenario 1 in light and dark taskbar and on a monitor at 150%
    scaling.
