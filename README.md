# KidzDev Unity Text Scroll

Animated text for uGUI/TMPro — four modes, two independent component families:

- **Marquee** — continuously looping horizontal/vertical ticker.
- **Credits** — one-pass bottom-to-top roll.
- **Auto-fit** — scrolls only when content overflows its container; stays put otherwise.
- **Typewriter** — character-by-character reveal, rich-text safe.

No third-party animation dependency — all motion is per-frame stepping. No coupling to any other KidzDev package.

## Installation

Open **Package Manager → + → Add package from git URL** and enter:

```
https://github.com/knabsiraphop/kidzdev-unity-text-scroll.git#v0.1.0
```

Or add to `Packages/manifest.json`:

```json
"com.kidzdev.unity.text-scroll": "https://github.com/knabsiraphop/kidzdev-unity-text-scroll.git#v0.1.0"
```

Requires `com.cysharp.unitask` (OpenUPM), `com.unity.ugui`, and TextMeshPro.

## `TextScroller` — position scrolling (marquee / credits / auto-fit)

Add to a `RectTransform` that holds the scrolling text, inside a masked viewport (`RectMask2D` or `Mask`):

```csharp
using KidzDev.Unity.TextScroll;

var scroller = GetComponent<TextScroller>();
scroller.Mode = ScrollMode.Marquee;
scroller.Axis = ScrollAxis.Horizontal;
scroller.Speed = 80f; // units/sec

scroller.Play();
scroller.OnCycleComplete += () => Debug.Log("wrapped");

// Credits / AutoFit: await a single pass
await scroller.PlayToEndAsync(cancellationToken);
```

- **Marquee** loops continuously; wraps seamlessly once the trailing edge clears the viewport.
- **Credits** starts below the viewport, scrolls up until fully clear, then completes (or loops if `Loop` is set).
- **AutoFit** measures content vs. viewport on `Play()`. Fits → no movement. Overflows → scrolls per `OverflowBehavior` (`PingPong` / `LoopWithGap` / `None`).

Viewport clipping is your responsibility — put a `RectMask2D`/`Mask` on the viewport RectTransform.

## `Typewriter` — character reveal

Add to a `TMP_Text`:

```csharp
using KidzDev.Unity.TextScroll;

var typewriter = GetComponent<Typewriter>();
typewriter.CharsPerSecond = 30f;

await typewriter.PlayAsync(cancellationToken);
// or reveal new text:
await typewriter.PlayAsync("Hello, <b>world</b>!", cancellationToken);

// Skip to the end at any time:
typewriter.Skip();
```

Uses `TMP_Text.maxVisibleCharacters` under the hood — rich-text tags aren't counted as visible characters, so `<b>`, `<color>`, etc. work without any tag parsing. `RevealUnit.PerWord` advances to word boundaries instead of one character at a time.

## Substitutable seams

Following the toolkit's facade + seam pattern — these are scene-configured `MonoBehaviour`s, not global services, so there's no static `.Default` facade; the seam sits on the swappable per-instance behavior:

- **`IScrollBehavior`** — the *how* of position scrolling. Default implementations: `MarqueeBehavior`, `CreditsBehavior`, `AutoFitBehavior`. Implement your own for a custom motion curve.
- **`IRevealStrategy`** — the *how* of character reveal. Default `VisibleCountReveal` (zero-alloc, rich-text safe). Implement your own (e.g. a per-char fade) and assign it to `Typewriter.RevealStrategy`.

## Samples

| Sample | Description |
| --- | --- |
| **Demo** | `TextScrollDemo.unity` — 1920x1080 scene, one panel per mode: marquee ticker, credits roll, auto-fit title (short vs. long), typewriter dialogue box with Skip. No extra package dependencies. |

Import via **Package Manager → KidzDev Unity Text Scroll → Samples**.

## License

MIT — see [LICENSE.md](LICENSE.md).
