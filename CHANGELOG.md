# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `TextScroller` component — position scrolling for `ScrollMode.Marquee` (continuous looping ticker), `ScrollMode.Credits` (one-pass bottom-to-top roll), and `ScrollMode.AutoFit` (scrolls only when content overflows the viewport). `ScrollAxis` (Horizontal/Vertical), `Play`/`Pause`/`Resume`/`Stop`, `PlayToEndAsync(CancellationToken)`, `OnCycleComplete` event.
- `IScrollBehavior` seam — `MarqueeBehavior`, `CreditsBehavior`, `AutoFitBehavior` (with `OverflowBehavior`: PingPong / LoopWithGap / None) default implementations.
- `Typewriter` component — character reveal over a `TMP_Text` via `maxVisibleCharacters` (rich-text safe), `RevealUnit` (PerCharacter/PerWord), `PlayAsync(CancellationToken)`, `PlayAsync(string, CancellationToken)`, `Skip()`, `OnCharRevealed`/`OnComplete` events.
- `IRevealStrategy` seam — `VisibleCountReveal` default implementation.
- `TextMeasure` utility — content-vs-viewport overflow measurement shared by `AutoFitBehavior` and tests.
- EditMode tests: `TextMeasure`, `AutoFitBehavior`, `MarqueeBehavior`, `CreditsBehavior`, `Typewriter` reveal logic (fake clock).
- PlayMode tests: `TextScroller` real-frame movement + `OnCycleComplete`; `Typewriter.PlayAsync` real-time reveal, `Skip()`, and cancellation.
- Demo sample (`Samples~/Demo`) — 1920x1080 scene covering all four modes.
