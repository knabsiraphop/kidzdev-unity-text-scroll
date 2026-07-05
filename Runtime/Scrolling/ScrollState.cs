using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Carries everything an <see cref="IScrollBehavior"/> needs to advance a scroll — config, measured
    /// sizes, and per-behavior scratch state — so behaviors stay pure-ish and testable without a live scene.
    /// </summary>
    public sealed class ScrollState
    {
        // ── config (set by TextScroller before Begin) ───────────────────────────
        public RectTransform Content;
        public RectTransform Viewport;
        public ScrollAxis Axis;
        public float Speed;
        public float Gap;
        public bool Loop;
        public OverflowBehavior OverflowBehavior;

        // ── measured at Begin ─────────────────────────────────────────────────
        public float ContentSize;
        public float ViewportSize;
        public bool Overflowing;

        // ── driven by Tick ────────────────────────────────────────────────────
        /// <summary>Cumulative travel distance from origin, in content-space units. Always &gt;= 0.</summary>
        public float Position;
        /// <summary>Set true by Tick exactly on the frame a loop/pass boundary is crossed; the caller reads and clears it.</summary>
        public bool CycleComplete;

        // ── AutoFit-only scratch ──────────────────────────────────────────────
        public float Direction = 1f;
        public bool Paused;
        public float PauseElapsed;
    }
}
