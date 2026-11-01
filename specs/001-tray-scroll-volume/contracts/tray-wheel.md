# Contract: Wheel over the tray icon

| Input (pointer over Sonar Flyout's tray icon) | Result |
| --- | --- |
| One notch up | Master +2 points (max 100); if muted, unmuted |
| One notch down | Master −2 points (min 0); mute state unchanged |
| Partial notches (touchpad, smooth wheel) | Summed; one step per 120 units of delta |
| Horizontal wheel | Ignored |
| Any wheel with the pointer elsewhere | No effect; input reaches other apps as usual |
| Any wheel while Sonar is unreachable | No effect, no error shown |

After each applied change: the tray icon glyph and tooltip show the new level, and an open flyout
shows the new Master value and the rescaled channel values. No window opens, closes or takes focus.

Sonar calls (existing endpoints only): `GET volumeSettings/classic`,
`PUT volumeSettings/classic/Master/Mute/false` (only when unmuting),
`PUT volumeSettings/classic/Master/Volume/<value>`.
