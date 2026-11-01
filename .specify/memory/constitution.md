# Sonar Flyout Constitution

## Core Principles

### I. Native Microsoft Stack Only

- The app MUST be built only on what Microsoft ships: WinUI 3 / Windows App SDK, .NET, the
  Windows SDK, and Win32 APIs called through `LibraryImport`.
- Third-party NuGet packages, UI frameworks, and helper libraries MUST NOT be added. Build-time
  tooling from Microsoft (MSIX tooling, `msstore` CLI) is allowed.
- Anything missing from the platform is implemented directly, in the smallest form that works.

Rationale: a native tray utility has no reason to carry dependencies; each one costs size, startup
time, AOT compatibility, and long-term maintenance.

### II. Fast and Light

- The app MUST stay Native AOT compatible and ship as a self-contained MSIX. No reflection-based
  code, runtime code generation, or APIs that break trimming.
- The flyout MUST open without visible delay or layout jumps, and interactions (hover, drag,
  click) MUST NOT lag, including after the PC has been idle.
- When the flyout is closed, the app MUST do no work beyond the tray icon's volume poll; no
  background timers, animations, or network calls beyond that.
- Any change that adds startup work, polling, or memory MUST say why in its plan.

Rationale: the app lives in the tray all day; it has to be invisible in Task Manager and instant
when used.

### III. Feels Like Windows

- UI MUST look and behave like Windows 11's own flyouts, menus, and tray icons: Fluent controls,
  system accent, acrylic, native corner radius, and placement relative to the taskbar.
- Light and dark theme MUST both work and follow the system setting live.
- Motion MUST be subtle and MUST respect the system "Animation effects" setting.
- Every control MUST be reachable by keyboard and expose a meaningful name to screen readers.

Rationale: users open it in place of the Windows volume flyout; anything that looks foreign or
behaves differently is a defect, not a style choice.

### IV. Localized Everywhere

- Every user-facing string (UI, tooltips, menus, errors, manifest display name and description)
  MUST come from MRT resources (`Strings/<lang>/Resources.resw` via `x:Uid` or `ResourceLoader`).
  Hard-coded user-facing text is not allowed.
- A new or changed string MUST be added to all shipped languages in the same change (en-US,
  de-DE, fr-FR, es-ES, it-IT, pt-BR, ru-RU, pl-PL, tr-TR, ja-JP, ko-KR, zh-Hans, zh-Hant).
- Layout MUST respect right-to-left flow direction and longer translated strings without clipping.
- Store listing text in `store/listings/` MUST be kept in step with user-visible feature changes.

Rationale: the app ships in 13 languages; a single untranslated string breaks that promise.

### V. Local-Only and Private

- The app MUST talk only to SteelSeries GG's local Sonar API, discovered through GG's own
  `coreProps.json` on the local machine. No other network endpoints.
- The app MUST NOT collect, store, or transmit telemetry, analytics, or personal data.
- When GG or Sonar is not running, not reachable, or changes port, the app MUST degrade
  gracefully: show a clear, localized message, keep the tray icon working, and recover on its
  own once Sonar is back. It MUST NOT crash or hang.

Rationale: `PRIVACY.md` promises no data collection, and Store certification runs without Sonar
installed.

### VI. Minimal Code

- Write the simplest implementation that does the job. No speculative abstraction, extra layers,
  or configuration that no current feature needs.
- Comments only where they explain a non-obvious *why* (a platform quirk, an external bug, a
  measured tradeoff). No narrating, restating, or change-history comments.
- Code is removed rather than left commented out or unused.

Rationale: a small codebase stays readable and keeps AOT size and review cost low.

### VII. Release Discipline

- The committed `Package.appxmanifest` keeps placeholder identity values. The release workflow
  stamps the real Store identity and version; real identity values MUST NOT be committed.
- A `vX.Y.Z` tag is the only way to release: it builds, submits to the Store, and creates the
  GitHub release. Each release MUST have a version higher than the last.
- Commits, PRs, and release notes MUST NOT contain AI attribution of any kind.
- The app is an independent project. Listings, README, and UI MUST NOT imply affiliation with
  or endorsement by SteelSeries, and MUST keep the existing non-affiliation disclaimer.

Rationale: releases go straight to the Store; a broken identity or a misleading listing fails
certification or harms users.

## Technical Constraints

- Target: Windows 11 (TargetPlatformMinVersion 10.0.22000.0), x64, .NET with
  `net*-windows10.0.26100.0`, WinUI 3, single-instance tray app.
- Packaging: one MSIX containing every language (no resource packs), `runFullTrust`, a
  `StartupTask` for start with Windows.
- Sonar integration: Classic mode endpoints only (`volumeSettings/classic`, `classicRedirections`,
  `audioDevices`, `linkAll`); the base address is rediscovered when a request fails.
- Assets: tray icons, logos, and Store art are generated by the scripts in `tools/`; edit the
  scripts, not the generated files.

## Development Workflow

- All changes go through Spec Kit: features via specify → (clarify) → plan → tasks → analyze →
  implement → converge; bugs via bug-assess → bug-fix → bug-test, where only a `verified`
  verdict counts as fixed. Each step is reviewed by the maintainer before the next.
- Every plan MUST include a Constitution Check against principles I–VII and justify any
  exception in its complexity tracking.
- A change is done only when it builds as MSIX in Release (Native AOT), has been run in the real
  app (installed via `tools/install-dev.ps1`) and checked against its acceptance scenarios, works in
  light and dark theme, and has all strings present in every shipped language.
- CI (`ci.yml`) MUST stay green on `main`.

## Governance

- This constitution overrides other practices in this repository. `CLAUDE.md` holds day-to-day
  workflow instructions and MUST NOT contradict it.
- Amendments are made with `/speckit-constitution`, reviewed by the maintainer, and committed
  on their own with a summary of what changed.
- Versioning: MAJOR for removing or redefining a principle, MINOR for adding a principle or
  materially expanding guidance, PATCH for wording and clarifications.
- Plans, task lists, and reviews MUST check compliance; a violation is either fixed or recorded
  as a justified exception in the plan.

**Version**: 1.0.0 | **Ratified**: 2026-11-01 | **Last Amended**: 2026-11-01
