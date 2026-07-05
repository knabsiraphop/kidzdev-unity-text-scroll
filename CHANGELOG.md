# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-05

First stable release.

### Added

- `TextScroller` component — position scrolling for `ScrollMode.Marquee` (continuous looping ticker), `ScrollMode.Credits` (one-pass bottom-to-top roll), and `ScrollMode.AutoFit` (scrolls only when content overflows the viewport). `ScrollAxis` (Horizontal/Vertical), `Play`/`Pause`/`Resume`/`Stop`, `PlayToEndAsync(CancellationToken)`, `OnCycleComplete` event, and a `Seamless` clone mode for gap-free marquee looping.
- `IScrollBehavior` seam — `MarqueeBehavior`, `CreditsBehavior`, `AutoFitBehavior` (with `OverflowBehavior`: PingPong / LoopWithGap / None) default implementations.
- `Typewriter` component — character reveal over a `TMP_Text` via `maxVisibleCharacters` (rich-text safe), `RevealUnit` (PerCharacter/PerWord), `TypewriterOptions` (chars/sec, unit, punctuation pause), `PlayAsync(CancellationToken)`, `PlayAsync(string, CancellationToken)`, `Skip()`, `OnCharRevealed`/`OnComplete` events.
- `IRevealStrategy` seam — `VisibleCountReveal` (default, zero-alloc), `FadeReveal` (per-character alpha fade-in), and `ScrambleReveal` (decode/hacker-terminal effect) implementations.
- `RevealCursor` — pure, unit-testable reveal-progression logic shared by all reveal strategies.
- `NumberRoller` component — animated count-up/odometer label; `AnimateToAsync(double, CancellationToken)`, `SetImmediate`, `RollEase` (Linear/EaseOutQuad/EaseOutCubic), configurable `Duration`/`Format`.
- `TextMeasure` utility — content-vs-viewport overflow measurement shared by `AutoFitBehavior` and tests.
- `TimeMode` (Scaled/Unscaled) on `TextScroller`, `Typewriter`, and `NumberRoller` for consistent pause-independent or pause-respecting animation.
- Shared internal `TextScrollDriver` — a single batched per-frame pump that ticks every active `TextScroller`/`NumberRoller` instead of one `MonoBehaviour.Update` each; idle components cost nothing and register only while animating.
- EditMode tests: `TextMeasure`, `AutoFitBehavior`, `MarqueeBehavior`, `CreditsBehavior`, `Typewriter`/`RevealCursor` reveal logic (fake clock), `TextScrollDriver` registration.
- PlayMode tests: `TextScroller` real-frame movement + `OnCycleComplete`; `Typewriter.PlayAsync` real-time reveal, `Skip()`, and cancellation; `NumberRoller` real-time animation; `TextScrollDriver` end-to-end pump.
- Demo sample (`Samples~/Demo`) — 1920x1080 scene covering all four modes.

### Fixed

- Replay/restart position drift, marquee entry offset, and credits anchor placement on repeated `Play()` calls.
- Overlapping layout in the Demo scene's Auto-Fit panel.
- Missing Main Camera in the Demo scene.
